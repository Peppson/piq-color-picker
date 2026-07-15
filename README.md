
# PiQ - Color Picker&nbsp;<img src="Images/Logo/logo.png" alt="logo" width="34"/>  
 
Lightweight desktop color picker for Windows. Capture any screen pixel, fine-tune the selection with mouse or keyboard after capture, and copy colors in various formats.

<a href="https://apps.microsoft.com/detail/9NRQNX8H7NQ1?referrer=appbadge&mode=full" target="_blank"  rel="noopener noreferrer">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

<br>   
&nbsp;


![App1](/Images/Screenshots/v1.0.2/github/1.png)  
 

## Features
- Capture colors from any pixel on your screen.
- Select the exact pixel with mouse or keyboard post-capture.
- Global hotkey to start and stop capture.
- Always-on-top window option.
- Intuitive, and easy-to-use _(i hope...)_
- Persistent settings.


## Screenshots
  
![App2](/Images/Screenshots/v1.0.2/github/2.png)  

![App3](/Images/Screenshots/v1.0.2/github/3.png)  


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
