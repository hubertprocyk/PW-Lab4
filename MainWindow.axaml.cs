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

}