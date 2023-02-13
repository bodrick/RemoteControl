﻿using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Reactive;
using Immense.RemoteControl.Desktop.UI.WPF.Services;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;
using Color = System.Windows.Media.Color;

namespace Immense.RemoteControl.Desktop.UI.WPF.ViewModels;

public interface IBrandedViewModelBase
{
    BitmapImage? Icon { get; set; }

    string? ProductName { get; set; }

    SolidColorBrush? TitleBackgroundColor { get; set; }

    SolidColorBrush? TitleButtonForegroundColor { get; set; }

    SolidColorBrush? TitleForegroundColor { get; set; }

    Task ApplyBrandingAsync();
}

public abstract class BrandedViewModelBase : ObservableObjectEx, IBrandedViewModelBase
{
    private static BrandingInfoBase? _brandingInfo;
    private readonly IBrandingProvider _brandingProvider;
    private readonly ILogger<BrandedViewModelBase> _logger;
    private readonly IWindowsUiDispatcher _dispatcher;

    public BrandedViewModelBase(
        IBrandingProvider brandingProvider,
        IWindowsUiDispatcher dispatcher,
        ILogger<BrandedViewModelBase> logger)
    {
        _brandingProvider = brandingProvider;
        _dispatcher = dispatcher;
        _logger = logger;
        _ = Task.Run(ApplyBrandingAsync);
    }

    public BitmapImage? Icon
    {
        get => Get<BitmapImage?>();
        set => Set(value);
    }

    public string? ProductName
    {
        get => Get<string?>();
        set => Set(value);
    }

    public SolidColorBrush? TitleBackgroundColor
    {
        get => Get<SolidColorBrush?>();
        set => Set(value);
    }

    public SolidColorBrush? TitleButtonForegroundColor
    {
        get => Get<SolidColorBrush?>();
        set => Set(value);
    }

    public SolidColorBrush? TitleForegroundColor
    {
        get => Get<SolidColorBrush?>();
        set => Set(value);
    }

    public async Task ApplyBrandingAsync()
    {
        await _dispatcher.InvokeWpfAsync(async () =>
        {
            try
            {
                _brandingInfo ??= await _brandingProvider.GetBrandingInfoAsync();

                ProductName = "Remote Control";

                if (!string.IsNullOrWhiteSpace(_brandingInfo.Product))
                {
                    ProductName = _brandingInfo.Product;
                }

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

                Icon = GetBitmapImageIcon(_brandingInfo);

                OnPropertyChanged(nameof(ProductName));
                OnPropertyChanged(nameof(TitleBackgroundColor));
                OnPropertyChanged(nameof(TitleForegroundColor));
                OnPropertyChanged(nameof(TitleButtonForegroundColor));
                OnPropertyChanged(nameof(Icon));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying branding.");
            }
        });
    }

    private BitmapImage GetBitmapImageIcon(BrandingInfoBase bi)
    {
        try
        {
            Stream imageStream;
            if (bi.Icon?.Any() == true)
            {
                imageStream = new MemoryStream(bi.Icon);
            }
            else
            {
                imageStream = typeof(IAppStartup)
                    .Assembly
                    .GetManifestResourceStream("Immense.RemoteControl.Desktop.Shared.Assets.DefaultIcon.png") ?? new MemoryStream();
            }

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
            _logger.LogError(ex, "Error while getting app icon.");
            return new BitmapImage();
        }
    }
}
