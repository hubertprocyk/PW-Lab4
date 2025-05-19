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


}