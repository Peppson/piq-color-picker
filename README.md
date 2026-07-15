
<p align="center">
	<img src="Images/Logo/logo-1000x1000.png" alt="Logo" width="74" />
</p>

<h1 align="center">
	PiQ - Color Picker 
	<p>
		<img src="https://img.shields.io/github/v/release/Peppson/piq-color-picker?style=flat-square&label=Release&color=5596FB" alt="Release">
		<img src="https://img.shields.io/badge/Fun_hobby_project%3F-yes-A3E061?style=flat-square" alt="Dumb hobby project? Yes">
	</p>
</h1>

Lightweight color picker for Windows. Capture any pixel on your screen, fine-tune the selection with mouse or keyboard after capture, and copy colors in various formats.  

<a href="https://apps.microsoft.com/detail/9NRQNX8H7NQ1?referrer=appbadge&mode=full" target="_blank" rel="noopener noreferrer">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="210" alt="Download from Microsoft Store" />
</a>


<br>
&nbsp;


![App1](/Images/Screenshots/v1.0.3/1.png)  


## Features
- Capture colors from any pixel on your screen.
- Select the exact pixel with mouse or keyboard post-capture.
- Global hotkey to start and stop capture.
- GPU-accelerated capture with GDI fallback.
- Always-on-top window option.
- Persistent settings.  


## Screenshots
  
![App2](/Images/Screenshots/v1.0.3/2.png)  

![App3](/Images/Screenshots/v1.0.3/3.png)  

![App4](/Images/Screenshots/v1.0.3/4.png)  


## Installation

#### Requirements:
- Windows 10+
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet) *(to build or run from source)*
  
#### Microsoft Store
<a href="https://apps.microsoft.com/detail/9NRQNX8H7NQ1?referrer=appbadge&mode=full" target="_blank"  rel="noopener noreferrer">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

#### Download portable .exe _(unsigned)_
- Download the latest release from [Releases](https://github.com/Peppson/piq-color-picker/releases) page  

#### Build portable .exe from source
```bash
git clone "https://github.com/Peppson/piq-color-picker"
cd "piq-color-picker/ColorPicker"
dotnet publish ColorPicker/ColorPicker.csproj -c Release -p:PublishProfile=SelfContainedExe -o "$HOME/Desktop"
```

#### Run from Source
```bash
git clone "https://github.com/Peppson/piq-color-picker"
cd "piq-color-picker/ColorPicker"
dotnet run -c Release 
```

<br>
