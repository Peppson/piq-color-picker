
<p align="center">
	<img src="Images/Logo/logo-1000x1000.png" alt="PiQ logo" width="70" />
</p>

<h1 align="center" style="margin-top: -30px;">PiQ - Color Picker</h1>



<h1 align="center">
  <img src="Images/Logo/logo-1000x1000.png" width="70" valign="middle">

  PiQ - Color Picker  
  
</h1>

<!-- <p align="center">
	<img src="https://img.shields.io/badge/version-1.0.3-5495FB?style=flat-square" alt="Version 1.0.3" />
	<a href="https://github.com/Peppson/piq-color-picker/releases/latest">
		<img src="https://img.shields.io/github/v/release/Peppson/piq-color-picker?style=flat-square&label=release&color=3D8BFF" alt="Latest release" />
	</a>
</p> -->



Lightweight color picker for Windows. Capture any pixel on your screen, fine-tune the selection with mouse or keyboard after capture, and copy colors in various formats.


<a href="https://apps.microsoft.com/detail/9NRQNX8H7NQ1?referrer=appbadge&mode=full" target="_blank" rel="noopener noreferrer">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200" alt="Download from Microsoft Store" />
</a>


<br>
&nbsp;


![App1](/Images/Screenshots/v1.0.3/1.png)  

 

## Features
- Capture colors from any pixel on your screen.
- Select the exact pixel with mouse or keyboard post-capture.
- Global hotkey to start and stop capture.
- Always-on-top window option.
- Intuitive, and easy-to-use _(i hope...)_
- Persistent settings.


## Screenshots
  
![App2](/Images/Screenshots/v1.0.3/2.png)  

![App2](/Images/Screenshots/v1.0.3/3.png)  

![App2](/Images/Screenshots/v1.0.3/4.png)  


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
