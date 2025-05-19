using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace PW_Lab4;

public partial class MainWindow : Window
{
    private WriteableBitmap? _editableBitmap;

    public MainWindow()
    {
        InitializeComponent();
    }

    [Obsolete("Obsolete")]
    private async void OnLoadImageClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            AllowMultiple = false,
            Filters =
            {
                new FileDialogFilter { Name = "Obrazy", Extensions = { "png", "jpg", "jpeg", "bmp" } }
            }
        };

        var result = await dialog.ShowAsync(this);
        if (result is null || result.Length == 0)
            return;

        var filePath = result[0];
        await LoadImage(filePath);
    }

    private async Task LoadImage(string path)
    {
        await using var stream = File.OpenRead(path);
        var original = new Bitmap(stream);

        _editableBitmap = new WriteableBitmap(
            new PixelSize(original.PixelSize.Width, original.PixelSize.Height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using (var fb = _editableBitmap.Lock())
        {
            original.CopyPixels(
                new PixelRect(0, 0, original.PixelSize.Width, original.PixelSize.Height),
                fb.Address,
                fb.RowBytes * fb.Size.Height,
                fb.RowBytes);
        }

        ImageView.Source = _editableBitmap;
    }
    
    private async Task RunWithSpinner(Func<Task> action)
    {
        LoadingOverlay.IsVisible = true;
        await Task.Delay(50); // Daj czas na przerysowanie UI

        await Task.Run(action);

        LoadingOverlay.IsVisible = false;
    }
    
    private async void OnOnlyGreenClick(object? sender, RoutedEventArgs e)
    {
        if (_editableBitmap is null) return;

        await RunWithSpinner(() =>
        {
            using var fb = _editableBitmap.Lock();

            unsafe
            {
                var ptr = (uint*)fb.Address;
                int pixelCount = fb.Size.Width * fb.Size.Height;

                for (int i = 0; i < pixelCount; i++)
                {
                    uint pixel = ptr[i];

                    byte a = (byte)(pixel >> 24);
                    byte g = (byte)(pixel >> 8);

                    byte r = 0;
                    byte b = 0;

                    ptr[i] = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
                }
            }

            return Task.CompletedTask;
        });

        ImageView.Source = _editableBitmap;
    }
    
    private void OnRotateClick(object? sender, RoutedEventArgs e)
    {
        if (_editableBitmap is null)
            return;

        var angle = Rotate90.IsChecked == true ? 90 :
                    Rotate180.IsChecked == true ? 180 :
                    Rotate270.IsChecked == true ? 270 : 0;

        _editableBitmap = RotateBitmap(_editableBitmap, angle);
        ImageView.Source = _editableBitmap;
    }

    private static WriteableBitmap RotateBitmap(WriteableBitmap src, int angle)
    {
        var srcPixelSize = src.PixelSize;
        var srcWidth = srcPixelSize.Width;
        var srcHeight = srcPixelSize.Height;

        var destWidth = (angle == 180) ? srcWidth : srcHeight;
        var destHeight = (angle == 180) ? srcHeight : srcWidth;

        var dst = new WriteableBitmap(
            new PixelSize(destWidth, destHeight),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using var srcBuf = src.Lock();
        using var dstBuf = dst.Lock();

        unsafe
        {
            var srcPtr = (uint*)srcBuf.Address;
            var dstPtr = (uint*)dstBuf.Address;

            for (var y = 0; y < srcHeight; y++)
            {
                for (var x = 0; x < srcWidth; x++)
                {
                    var srcIndex = y * srcBuf.RowBytes / 4 + x;
                    var color = srcPtr[srcIndex];

                    int dstX, dstY;

                    switch (angle)
                    {
                        case 90:
                            dstX = srcHeight - 1 - y;
                            dstY = x;
                            break;
                        case 180:
                            dstX = srcWidth - 1 - x;
                            dstY = srcHeight - 1 - y;
                            break;
                        case 270:
                            dstX = y;
                            dstY = srcWidth - 1 - x;
                            break;
                        default:
                            dstX = x;
                            dstY = y;
                            break;
                    }

                    var dstIndex = dstY * dstBuf.RowBytes / 4 + dstX;
                    dstPtr[dstIndex] = color;
                }
            }
        }

        return dst;
    }
    
    private async void OnInvertColorsClick(object? sender, RoutedEventArgs e)
    {
        if (_editableBitmap is null) return;

        await RunWithSpinner(() =>
        {
            using var fb = _editableBitmap.Lock();
            unsafe
            {
                var ptr = (uint*)fb.Address;
                int pixelCount = fb.Size.Width * fb.Size.Height;

                for (int i = 0; i < pixelCount; i++)
                {
                    uint pixel = ptr[i];

                    byte a = (byte)(pixel >> 24);
                    byte r = (byte)(pixel >> 16);
                    byte g = (byte)(pixel >> 8);
                    byte b = (byte)(pixel);

                    r = (byte)(255 - r);
                    g = (byte)(255 - g);
                    b = (byte)(255 - b);

                    ptr[i] = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
                }
            }

            return Task.CompletedTask;
        });

        ImageView.Source = _editableBitmap;
    }

    
    private async void OnFlipHorizontalClick(object? sender, RoutedEventArgs e)
    {
        if (_editableBitmap is null) return;

        await RunWithSpinner(() =>
        {
            using var fb = _editableBitmap.Lock();
            int width = fb.Size.Width;
            int height = fb.Size.Height;

            unsafe
            {
                var ptr = (uint*)fb.Address;
                int rowStride = fb.RowBytes / 4;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width / 2; x++)
                    {
                        int leftIndex = y * rowStride + x;
                        int rightIndex = y * rowStride + (width - 1 - x);

                        (ptr[leftIndex], ptr[rightIndex]) = (ptr[rightIndex], ptr[leftIndex]);
                    }
                }
            }

            return Task.CompletedTask;
        });

        ImageView.Source = _editableBitmap;
    }

   
}