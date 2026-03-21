using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OBS.Helpers
{
    /// <summary>
    /// Animasyon yardımcı sınıfı. Hem kod-arkası (code-behind) geçiş animasyonlarını
    /// hem de XAML üzerinden kullanılabilen modüler kart animasyonlarını içerir.
    /// </summary>
    public static class AnimationHelper
    {
        // ── 1. KOD ARKASI GEÇİŞ ANİMASYONLARI (Code-Behind Transitions) ──
        
        /// <summary>
        /// Bir UI elemanını (örneğin bir sayfayı veya paneli) aşağıdan yukarıya doğru 
        /// hafifçe kaydırarak ve saydamlığını artırarak ekrana getirir.
        /// </summary>
        public static void PlaySlideUpTransition(UIElement element)
        {
            PlayTransition(element, new Point(0, 15));
        }

        /// <summary>
        /// Bir UI elemanını sağdan sola doğru kaydırarak ekrana getirir.
        /// </summary>
        public static void PlaySlideFromRightTransition(UIElement element)
        {
            PlayTransition(element, new Point(30, 0));
        }

        /// <summary>
        /// Bir UI elemanını soldan sağa doğru kaydırarak ekrana getirir.
        /// </summary>
        public static void PlaySlideFromLeftTransition(UIElement element)
        {
            PlayTransition(element, new Point(-30, 0));
        }

        /// <summary>
        /// Verilen başlangıç ofsetine (startOffset) göre bir UI elemanını orijinal konumuna
        /// (0,0) doğru kaydırır ve paralel olarak saydamlığını (Opacity) 0'dan 1'e çıkarır.
        /// </summary>
        private static void PlayTransition(UIElement element, Point startOffset)
        {
            var transformGroup = new TransformGroup();
            var translateTransform = new TranslateTransform(startOffset.X, startOffset.Y);
            transformGroup.Children.Add(translateTransform);
            
            element.RenderTransform = transformGroup;
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            var sb = new Storyboard();

            var opacityAnim = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(opacityAnim, element);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));

            var translateXAnim = new DoubleAnimation
            {
                From = startOffset.X,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(translateXAnim, element);
            Storyboard.SetTargetProperty(translateXAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.X)"));

            var translateYAnim = new DoubleAnimation
            {
                From = startOffset.Y,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(translateYAnim, element);
            Storyboard.SetTargetProperty(translateYAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)"));

            sb.Children.Add(opacityAnim);
            
            if (startOffset.X != 0)
                sb.Children.Add(translateXAnim);
                
            if (startOffset.Y != 0)
                sb.Children.Add(translateYAnim);
            
            sb.Begin();
        }

        // ── 2. MODÜLER KART ANİMASYONLARI (Attached Properties) ──

        #region IsSlideUpEnabled Property

        /// <summary>
        /// Element yüklendiğinde (Loaded) aşağıdan yukarıya yumuşak bir giriş animasyonu oynatır.
        /// </summary>
        public static readonly DependencyProperty IsSlideUpEnabledProperty = DependencyProperty.RegisterAttached(
            "IsSlideUpEnabled", typeof(bool), typeof(AnimationHelper), new PropertyMetadata(false, OnIsSlideUpEnabledChanged));

        public static bool GetIsSlideUpEnabled(DependencyObject obj) => (bool)obj.GetValue(IsSlideUpEnabledProperty);
        public static void SetIsSlideUpEnabled(DependencyObject obj, bool value) => obj.SetValue(IsSlideUpEnabledProperty, value);

        private static void OnIsSlideUpEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && (bool)e.NewValue)
            {
                element.Loaded += Element_Loaded;
            }
            else if (d is FrameworkElement el)
            {
                el.Loaded -= Element_Loaded;
            }
        }

        private static async void Element_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                int delay = GetStaggerDelay(element);
                
                // Başlangıç durumu (Görünmez ve aşağıda)
                element.Opacity = 0;
                var transform = new TranslateTransform(0, 20);
                element.RenderTransform = transform;
                element.RenderTransformOrigin = new Point(0.5, 0.5);

                // Şelale (Stagger) gecikmesi
                if (delay > 0)
                {
                    await Task.Delay(delay);
                }

                // Element bu süreçte ekrandan kaldırıldıysa veya siliniyorsa animasyonu oynatma
                if (GetIsRemoving(element) || !element.IsLoaded) return;

                // Giriş animasyonları
                var sb = new Storyboard();
                
                var opacityAnim = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromMilliseconds(500) };
                Storyboard.SetTarget(opacityAnim, element);
                Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));

                var translateAnim = new DoubleAnimation { From = 20, To = 0, Duration = TimeSpan.FromMilliseconds(500) };
                translateAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                Storyboard.SetTarget(translateAnim, element);
                Storyboard.SetTargetProperty(translateAnim, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

                sb.Children.Add(opacityAnim);
                sb.Children.Add(translateAnim);
                sb.Begin();
            }
        }

        #endregion

        #region StaggerDelay Property

        /// <summary>
        /// Animasyonun başlaması için beklenecek milisaniye cinsinden gecikme süresi.
        /// </summary>
        public static readonly DependencyProperty StaggerDelayProperty = DependencyProperty.RegisterAttached(
            "StaggerDelay", typeof(int), typeof(AnimationHelper), new PropertyMetadata(0));

        public static int GetStaggerDelay(DependencyObject obj) => (int)obj.GetValue(StaggerDelayProperty);
        public static void SetStaggerDelay(DependencyObject obj, int value) => obj.SetValue(StaggerDelayProperty, value);

        #endregion

        #region IsRemoving Property

        /// <summary>
        /// True yapıldığında elementin aşağı kayarak kaybolmasını (çıkış animasyonu) sağlar.
        /// </summary>
        public static readonly DependencyProperty IsRemovingProperty = DependencyProperty.RegisterAttached(
            "IsRemoving", typeof(bool), typeof(AnimationHelper), new PropertyMetadata(false, OnIsRemovingChanged));

        public static bool GetIsRemoving(DependencyObject obj) => (bool)obj.GetValue(IsRemovingProperty);
        public static void SetIsRemoving(DependencyObject obj, bool value) => obj.SetValue(IsRemovingProperty, value);

        private static void OnIsRemovingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && (bool)e.NewValue)
            {
                var transform = element.RenderTransform as TranslateTransform;
                if (transform == null)
                {
                    transform = new TranslateTransform(0, 0);
                    element.RenderTransform = transform;
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                }

                var sb = new Storyboard();

                var opacityAnim = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(500) };
                Storyboard.SetTarget(opacityAnim, element);
                Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));

                var translateAnim = new DoubleAnimation { To = 50, Duration = TimeSpan.FromMilliseconds(500) };
                translateAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
                Storyboard.SetTarget(translateAnim, element);
                Storyboard.SetTargetProperty(translateAnim, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

                sb.Children.Add(opacityAnim);
                sb.Children.Add(translateAnim);
                sb.Begin();
            }
        }

        #endregion
    }
}
