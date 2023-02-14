using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Immense.RemoteControl.Shared.Models;
using Color = System.Windows.Media.Color;

namespace Immense.RemoteControl.Desktop.UI.WPF.ViewModels.Fakes;

public class FakeBrandedViewModelBase : IBrandedViewModelBase
{
    private readonly BrandingInfoBase _brandingInfo;

    public FakeBrandedViewModelBase()
    {
        _brandingInfo = new BrandingInfoBase();
        Icon = GetBitmapImageIcon(_brandingInfo);

        TitleBackgroundColor = new SolidColorBrush(Color.FromRgb(
            _brandingInfo.TitleBackgroundRed,
            _brandingInfo.TitleBackgroundGreen,
            _brandingInfo.TitleBackgroundBlue));

        TitleForegroundColor = new SolidColorBrush(Color.FromRgb(
           _brandingInfo.TitleForegroundRed,
           _brandingInfo.TitleForegroundGreen,
           _brandingInfo.TitleForegroundBlue));

        TitleButtonForegroundColor = new SolidColorBrush(Color.FromRgb(
           _brandingInfo.ButtonForegroundRed,
           _brandingInfo.ButtonForegroundGreen,
           _brandingInfo.ButtonForegroundBlue));
    }

    public BitmapImage? Icon { get; set; }

    public string? ProductName { get; set; } = "Test Product";

    public SolidColorBrush? TitleBackgroundColor { get; set; }

    public SolidColorBrush? TitleButtonForegroundColor { get; set; }

    public SolidColorBrush? TitleForegroundColor { get; set; }

    public Task ApplyBrandingAsync()
    {
        return Task.CompletedTask;
    }

    private BitmapImage GetBitmapImageIcon(BrandingInfoBase bi)
    {
        try
        {
            using var imageStream = typeof(Shared.Services.AppState)
                .Assembly
                .GetManifestResourceStream("Immense.RemoteControl.Desktop.Shared.Assets.DefaultIcon.png") ?? new MemoryStream();

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = imageStream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            imageStream.Close();

            return bitmap;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            return new BitmapImage();
        }
    }
}
