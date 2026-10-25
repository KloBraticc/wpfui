using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Wpf.Ui.Hardware;

namespace Wpf.Ui.Animations
{
    public static class AnimationState
    {
        public static bool IsLoading { get; set; }
    }

    public static class Transitions
    {
        public static readonly DependencyProperty HoldStillProperty = DependencyProperty.RegisterAttached("HoldStill", typeof(bool), typeof(Transitions), new PropertyMetadata(false, OnHoldStillChanged));

        private static readonly List<WeakReference<FrameworkElement>> HeldElements = new();

        public static bool GetHoldStill(DependencyObject element) => (bool)element.GetValue(HoldStillProperty);

        public static void SetHoldStill(DependencyObject element, bool value) => element.SetValue(HoldStillProperty, value);

        private static void OnHoldStillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element || e.NewValue is not true)
                return;

            HeldElements.RemoveAll(IsDeadReference);
            HeldElements.Add(new WeakReference<FrameworkElement>(element));
        }

        private static bool IsDeadReference(WeakReference<FrameworkElement> reference) => !reference.TryGetTarget(out _);

        private static List<TranslateTransform>? CollectHeldTransforms(FrameworkElement page, double offsetX, double offsetY)
        {
            List<TranslateTransform>? held = null;

            foreach (WeakReference<FrameworkElement> reference in HeldElements)
            {
                if (!reference.TryGetTarget(out FrameworkElement? element) || !GetHoldStill(element) || !element.IsDescendantOf(page))
                    continue;

                if (element.RenderTransform is not TranslateTransform transform || transform.IsFrozen)
                {
                    transform = new TranslateTransform();
                    element.RenderTransform = transform;
                }

                ResetAnimation(transform, TranslateTransform.XProperty, -offsetX);
                ResetAnimation(transform, TranslateTransform.YProperty, -offsetY);
                (held ??= new List<TranslateTransform>()).Add(transform);
            }

            return held;
        }
        private const int MinDuration = 250;
        private const int MaxDuration = 2000;
        private static readonly IEasingFunction FadeEase = CreateEase();
        private static readonly IEasingFunction MoveEase = CreateEase();
        private static readonly DependencyProperty IsTransitionTransformProperty =
            DependencyProperty.RegisterAttached(
                "IsTransitionTransform",
                typeof(bool),
                typeof(Transitions),
                new PropertyMetadata(false));
        private static readonly DependencyProperty AnimationGenerationProperty =
            DependencyProperty.RegisterAttached(
                "AnimationGeneration",
                typeof(long),
                typeof(Transitions),
                new PropertyMetadata(0L));
        private static readonly DependencyProperty TransitionGenerationProperty =
            DependencyProperty.RegisterAttached(
                "TransitionGeneration",
                typeof(long),
                typeof(Transitions),
                new PropertyMetadata(0L));
        private static readonly DependencyProperty VisibilityTargetProperty =
            DependencyProperty.RegisterAttached(
                "VisibilityTarget",
                typeof(bool?),
                typeof(Transitions),
                new PropertyMetadata(null));

        public static bool ApplyVisibilityTransition(FrameworkElement? element, bool visible, int duration = 160)
        {
            if (element is null)
            {
                return false;
            }

            bool currentlyVisible = element.GetValue(VisibilityTargetProperty) is bool target
                ? target
                : element.Visibility == Visibility.Visible;

            if (currentlyVisible == visible)
            {
                if (!visible && element.Visibility != Visibility.Collapsed && !element.HasAnimatedProperties)
                {
                    element.Visibility = Visibility.Collapsed;
                }
                else if (visible && element.Visibility != Visibility.Visible)
                {
                    element.Visibility = Visibility.Visible;
                }

                return false;
            }

            element.SetValue(VisibilityTargetProperty, visible);
            long generation = NextTransitionGeneration(element);

            if (AnimationState.IsLoading ||
                HardwareAcceleration.AnimationsDisabled ||
                RenderReady.ReducedMotion ||
                !element.IsLoaded ||
                !element.Dispatcher.CheckAccess() ||
                element.Dispatcher.HasShutdownStarted ||
                element.Dispatcher.HasShutdownFinished)
            {
                Reset(element);
                element.IsHitTestVisible = visible;
                element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                return false;
            }

            duration = Math.Clamp(duration, 100, MaxDuration);

            try
            {
                TranslateTransform transform = GetTransitionTransform(element);
                if (visible)
                {
                    bool wasCollapsed = element.Visibility != Visibility.Visible;
                    double fromOpacity = wasCollapsed ? 0.0 : Math.Clamp(element.Opacity, 0.0, 1.0);
                    double fromX = wasCollapsed ? -12.0 : transform.X;
                    if (!wasCollapsed && fromOpacity >= 0.99 && Math.Abs(fromX) < 0.5)
                    {
                        fromOpacity = 0.0;
                        fromX = -12.0;
                    }

                    element.Visibility = Visibility.Visible;
                    element.IsHitTestVisible = true;
                    ResetAnimation(transform, TranslateTransform.XProperty, 0);
                    ResetAnimation(transform, TranslateTransform.YProperty, 0);
                    ResetAnimation(element, UIElement.OpacityProperty, 1);
                    RenderReady.Hold(element, TimeSpan.FromMilliseconds(duration + 60));
                    Animate(transform, TranslateTransform.XProperty, fromX, 0, duration, MoveEase);
                    Animate(element, UIElement.OpacityProperty, fromOpacity, 1, duration, FadeEase);
                    return true;
                }

                if (element.Visibility != Visibility.Visible)
                {
                    Reset(element);
                    element.IsHitTestVisible = false;
                    element.Visibility = Visibility.Collapsed;
                    return false;
                }

                if (element.IsKeyboardFocusWithin)
                {
                    System.Windows.Input.Keyboard.ClearFocus();
                }

                element.IsHitTestVisible = false;
                double closeFromOpacity = Math.Clamp(element.Opacity, 0.0, 1.0);
                if (closeFromOpacity <= 0.01)
                {
                    closeFromOpacity = 1.0;
                }

                double closeFromX = transform.X;
                const double closeToX = -12.0;

                ResetAnimation(transform, TranslateTransform.XProperty, closeToX);
                ResetAnimation(transform, TranslateTransform.YProperty, 0);
                ResetAnimation(element, UIElement.OpacityProperty, 0);
                RenderReady.Hold(element, TimeSpan.FromMilliseconds(duration + 60));
                Animate(transform, TranslateTransform.XProperty, closeFromX, closeToX, duration, MoveEase);
                Animate(element, UIElement.OpacityProperty, closeFromOpacity, 0, duration, FadeEase, () =>
                {
                    if (!IsCurrentTransition(element, generation))
                    {
                        return;
                    }

                    Reset(element);
                    element.Visibility = Visibility.Collapsed;
                });
                return true;
            }
            catch (InvalidOperationException)
            {
                Reset(element);
                element.IsHitTestVisible = visible;
                element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                return false;
            }
            catch (ArgumentException)
            {
                Reset(element);
                element.IsHitTestVisible = visible;
                element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                return false;
            }
        }

        public static bool ApplyTransition(object element, TransitionType type, int duration)
        {
            if (type == TransitionType.None ||
                element is not FrameworkElement frameworkElement ||
                AnimationState.IsLoading ||
                HardwareAcceleration.AnimationsDisabled ||
                !frameworkElement.Dispatcher.CheckAccess() ||
                frameworkElement.Dispatcher.HasShutdownStarted ||
                frameworkElement.Dispatcher.HasShutdownFinished)
            {
                return false;
            }

            duration = Math.Clamp(duration, MinDuration, MaxDuration);

            try
            {
                switch (type)
                {
                    case TransitionType.FadeIn:
                        FadeIn(frameworkElement, duration);
                        return true;
                    case TransitionType.SlideBottom:
                        Slide(frameworkElement, 0, 18, duration, false);
                        return true;
                    case TransitionType.SlideRight:
                        Slide(frameworkElement, 16, 0, duration, false);
                        return true;
                    case TransitionType.SlideLeft:
                        Slide(frameworkElement, -16, 0, duration, false);
                        return true;
                    case TransitionType.FadeInWithSlide:
                        Slide(frameworkElement, 0, 18, duration, true);
                        return true;
                    case TransitionType.FadeInWithSlideRight:
                        Slide(frameworkElement, 16, 0, duration, true);
                        return true;
                    case TransitionType.FadeInWithSlideLeft:
                        Slide(frameworkElement, -16, 0, duration, true);
                        return true;
                    default:
                        return false;
                }
            }
            catch (InvalidOperationException)
            {
                Reset(element: frameworkElement);
                return false;
            }
            catch (ArgumentException)
            {
                Reset(element: frameworkElement);
                return false;
            }
        }

        private static void FadeIn(FrameworkElement element, int durationMilliseconds)
        {
            long generation = NextTransitionGeneration(element);
            ResetAnimation(element, UIElement.OpacityProperty, 0);

            RenderReady.Run(element, () =>
            {
                if (!IsCurrentTransition(element, generation))
                    return;

                ResetAnimation(element, UIElement.OpacityProperty, 1);
                RenderReady.Hold(element, TimeSpan.FromMilliseconds(durationMilliseconds));
                Animate(element, UIElement.OpacityProperty, 0, 1, durationMilliseconds, FadeEase);
            });
        }

        private static void Slide(
            FrameworkElement element,
            double offsetX,
            double offsetY,
            int durationMilliseconds,
            bool fade)
        {
            long generation = NextTransitionGeneration(element);
            TranslateTransform transform = GetTransitionTransform(element);
            ResetAnimation(transform, TranslateTransform.XProperty, offsetX);
            ResetAnimation(transform, TranslateTransform.YProperty, offsetY);
            ResetAnimation(element, UIElement.OpacityProperty, fade ? 0 : 1);
            List<TranslateTransform>? held = CollectHeldTransforms(element, offsetX, offsetY);

            RenderReady.Run(element, () =>
            {
                if (!IsCurrentTransition(element, generation) || transform.IsFrozen)
                    return;

                if (held != null)
                {
                    foreach (TranslateTransform heldTransform in held)
                    {
                        ResetAnimation(heldTransform, TranslateTransform.XProperty, 0);
                        ResetAnimation(heldTransform, TranslateTransform.YProperty, 0);

                        if (offsetX != 0)
                            Animate(heldTransform, TranslateTransform.XProperty, -offsetX, 0, durationMilliseconds, MoveEase);

                        if (offsetY != 0)
                            Animate(heldTransform, TranslateTransform.YProperty, -offsetY, 0, durationMilliseconds, MoveEase);
                    }
                }

                ResetAnimation(transform, TranslateTransform.XProperty, 0);
                ResetAnimation(transform, TranslateTransform.YProperty, 0);
                ResetAnimation(element, UIElement.OpacityProperty, 1);
                RenderReady.Hold(element, TimeSpan.FromMilliseconds(durationMilliseconds));

                if (offsetX != 0)
                    Animate(transform, TranslateTransform.XProperty, offsetX, 0, durationMilliseconds, MoveEase);

                if (offsetY != 0)
                    Animate(transform, TranslateTransform.YProperty, offsetY, 0, durationMilliseconds, MoveEase);

                if (fade)
                    Animate(element, UIElement.OpacityProperty, 0, 1, durationMilliseconds, FadeEase);
            });
        }

        private static void Animate(
            Animatable target,
            DependencyProperty property,
            double from,
            double to,
            int durationMilliseconds,
            IEasingFunction easingFunction)
        {
            DoubleAnimation animation = new(from, to, TimeSpan.FromMilliseconds(durationMilliseconds))
            {
                EasingFunction = easingFunction,
                FillBehavior = FillBehavior.Stop
            };
            Timeline.SetDesiredFrameRate(animation, null);

            long generation = NextGeneration(target);
            WeakReference<Animatable> weakTarget = new(target);
            AnimationClock clock = (AnimationClock)animation.CreateClock(true);
            EventHandler? completed = null;
            completed = (_, _) =>
            {
                clock.Completed -= completed;
                try
                {
                    if (weakTarget.TryGetTarget(out Animatable? current) &&
                        (long)current.GetValue(AnimationGenerationProperty) == generation)
                    {
                        current.ApplyAnimationClock(property, null);
                    }
                }
                finally
                {
                    clock.Controller?.Remove();
                }
            };
            clock.Completed += completed;
            target.ApplyAnimationClock(property, clock, HandoffBehavior.SnapshotAndReplace);
        }

        private static void Animate(
            UIElement target,
            DependencyProperty property,
            double from,
            double to,
            int durationMilliseconds,
            IEasingFunction easingFunction,
            Action? onCompleted = null)
        {
            DoubleAnimation animation = new(from, to, TimeSpan.FromMilliseconds(durationMilliseconds))
            {
                EasingFunction = easingFunction,
                FillBehavior = FillBehavior.Stop
            };
            Timeline.SetDesiredFrameRate(animation, null);

            long generation = NextGeneration(target);
            WeakReference<UIElement> weakTarget = new(target);
            EventHandler? completed = null;
            AnimationClock clock = (AnimationClock)animation.CreateClock(true);
            completed = (_, _) =>
            {
                clock.Completed -= completed;
                try
                {
                    if (weakTarget.TryGetTarget(out UIElement? current) &&
                        (long)current.GetValue(AnimationGenerationProperty) == generation)
                    {
                        current.ApplyAnimationClock(property, null);
                        onCompleted?.Invoke();
                    }
                }
                finally
                {
                    clock.Controller?.Remove();
                }
            };
            clock.Completed += completed;
            target.ApplyAnimationClock(property, clock, HandoffBehavior.SnapshotAndReplace);
        }

        private static void ResetAnimation(Animatable target, DependencyProperty property, double value)
        {
            NextGeneration(target);
            target.ApplyAnimationClock(property, null);
            target.SetCurrentValue(property, value);
        }

        private static void ResetAnimation(UIElement target, DependencyProperty property, double value)
        {
            NextGeneration(target);
            target.ApplyAnimationClock(property, null);
            target.SetCurrentValue(property, value);
        }

        private static long NextGeneration(DependencyObject target)
        {
            long generation = unchecked((long)target.GetValue(AnimationGenerationProperty) + 1);
            target.SetValue(AnimationGenerationProperty, generation);
            return generation;
        }

        private static long NextTransitionGeneration(DependencyObject target)
        {
            long generation = unchecked((long)target.GetValue(TransitionGenerationProperty) + 1);
            target.SetValue(TransitionGenerationProperty, generation);
            return generation;
        }

        private static bool IsCurrentTransition(DependencyObject target, long generation)
        {
            return (long)target.GetValue(TransitionGenerationProperty) == generation;
        }

        private static void Reset(FrameworkElement element)
        {
            NextTransitionGeneration(element);
            ResetAnimation(element, UIElement.OpacityProperty, 1);

            if (FindTransitionTransform(element.RenderTransform) is not TranslateTransform transform || transform.IsFrozen)
                return;

            ResetAnimation(transform, TranslateTransform.XProperty, 0);
            ResetAnimation(transform, TranslateTransform.YProperty, 0);
        }

        private static TranslateTransform GetTransitionTransform(FrameworkElement element)
        {
            if (FindTransitionTransform(element.RenderTransform) is TranslateTransform existing && !existing.IsFrozen)
                return existing;

            TranslateTransform transform = new();
            transform.SetValue(IsTransitionTransformProperty, true);

            TransformGroup group = new();
            if (element.RenderTransform is Transform current)
                group.Children.Add(current);
            group.Children.Add(transform);
            element.SetCurrentValue(UIElement.RenderTransformProperty, group);
            return transform;
        }

        private static TranslateTransform? FindTransitionTransform(Transform? root)
        {
            if (root is TranslateTransform transform &&
                (bool)transform.GetValue(IsTransitionTransformProperty))
            {
                return transform;
            }

            if (root is not TransformGroup group)
                return null;

            foreach (Transform child in group.Children)
            {
                if (child is TranslateTransform candidate &&
                    (bool)candidate.GetValue(IsTransitionTransformProperty))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static CubicEase CreateEase()
        {
            CubicEase easing = new() { EasingMode = EasingMode.EaseOut };
            easing.Freeze();
            return easing;
        }
    }
}
