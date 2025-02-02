﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Hosting.Internal;

namespace Microsoft.Maui
{
	public static class FontsMauiAppBuilderExtensions
	{
		public static MauiAppBuilder ConfigureFonts(this MauiAppBuilder builder)
		{
			builder.ConfigureFonts(configureDelegate: null);
			return builder;
		}

		public static MauiAppBuilder ConfigureFonts(this MauiAppBuilder builder, Action<IFontCollection>? configureDelegate)
		{
			builder.Services.TryAddSingleton<IEmbeddedFontLoader>(svc => new EmbeddedFontLoader(svc.CreateLogger<EmbeddedFontLoader>()));
			builder.Services.TryAddSingleton<IFontRegistrar>(svc => new FontRegistrar(svc.GetRequiredService<IEmbeddedFontLoader>(), svc.CreateLogger<FontRegistrar>()));
			builder.Services.TryAddSingleton<IFontManager>(svc => new FontManager(svc.GetRequiredService<IFontRegistrar>(), svc.CreateLogger<FontManager>()));
			if (configureDelegate != null)
			{
				builder.Services.AddSingleton<FontsRegistration>(new FontsRegistration(configureDelegate));
			}
			builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IMauiInitializeService, FontInitializer>());
			return builder;
		}


		internal class FontsRegistration
		{
			private readonly Action<IFontCollection> _registerFonts;

			public FontsRegistration(Action<IFontCollection> registerFonts)
			{
				_registerFonts = registerFonts;
			}

			internal void AddFonts(IFontCollection fonts)
			{
				_registerFonts(fonts);
			}
		}

		internal class FontInitializer : IMauiInitializeService
		{
			private readonly IEnumerable<FontsRegistration> _fontsRegistrations;
			readonly IFontRegistrar _fontRegistrar;

			public FontInitializer(IEnumerable<FontsRegistration> fontsRegistrations, IFontRegistrar fontRegistrar)
			{
				_fontsRegistrations = fontsRegistrations;
				_fontRegistrar = fontRegistrar;
			}

			public void Initialize(IServiceProvider __)
			{
				if (_fontsRegistrations != null)
				{
					var fontsBuilder = new FontCollection();

					// Run all the user-defined registrations
					foreach (var font in _fontsRegistrations)
					{
						font.AddFonts(fontsBuilder);
					}

					// Register the fonts in the registrar
					foreach (var font in fontsBuilder)
					{
						if (font.Assembly == null)
							_fontRegistrar.Register(font.Filename, font.Alias);
						else
							_fontRegistrar.Register(font.Filename, font.Alias, font.Assembly);
					}
				}
			}
		}
	}
}
