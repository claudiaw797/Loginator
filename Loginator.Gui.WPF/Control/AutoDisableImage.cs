// Copyright (C) 2025 Claudia Wagner

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Loginator.Gui.WPF.Control {

    /// <summary>
    /// Origin https://www.renebergelt.de/blog/2019/10/automatically-grayscale-images-on-disabled-wpf-buttons/
    /// </summary>
    public class AutoDisableImage : Image {

        public double DisabledOpacity { get; set; } = 0.7;

        protected bool IsGrayscaled => Source is FormatConvertedBitmap;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoDisableImage"/> class.
        /// </summary>
        static AutoDisableImage() {
            // Override the metadata of the IsEnabled and Source properties to be notified of changes
            IsEnabledProperty.OverrideMetadata(
                typeof(AutoDisableImage),
                new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnAutoDisableImagePropertyChanged)));
            SourceProperty.OverrideMetadata(
                typeof(AutoDisableImage),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnAutoDisableImagePropertyChanged)));
        }

        /// <summary>
        /// Called when AutoDisableImage's IsEnabled or Source property values changed
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnAutoDisableImagePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs args) {
            if (source is AutoDisableImage me && me.IsEnabled == me.IsGrayscaled) {
                me.UpdateImage();
            }
        }

        protected void UpdateImage() {
            if (Source == null) return;

            if (IsEnabled) {
                // image is enabled (i.e. use the original image)

                if (IsGrayscaled) {
                    // restore the original image
                    Source = ((FormatConvertedBitmap)Source).Source;
                    // reset the Opcity Mask
                    OpacityMask = null;
                }
                Opacity = 1;
            }
            else {
                // image is disabled (i.e. grayscale the original image)
                if (!IsGrayscaled) {
                    // Get the source bitmap                        
                    if (Source is BitmapSource bitmapImage) {
                        //Source = new FormatConvertedBitmap(bitmapImage, PixelFormats.Gray8, null, 0);
                        Source = new FormatConvertedBitmap(bitmapImage, PixelFormats.Gray8, null, 0);
                        // reuse the opacity mask from the original image as FormatConvertedBitmap does not keep transparency info
                        OpacityMask = new ImageBrush(bitmapImage);
                    }
                }
                Opacity = DisabledOpacity;
            }
        }
    }
}
