
# Color Grab [![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download) [![WPF](https://img.shields.io/badge/WPF-512BD4?logo=windows&logoColor=white)](https://learn.microsoft.com/dotnet/desktop/wpf/)   

<!--
Todo 
<a href='//'><img src='https://developer.microsoft.com/store/badges/images/English_get-it-from-MS.png' alt='Ms Store' height='50px'/></a>
-->

Lightweight desktop color picker that grabs any pixel color from the mouse cursor.  
View and copy colors in `HEX`,`RGB`,`HSV`,`HSL` and `CMYK` formats, with zoom view for precise selection.  

![App1](/Images/Screenshots/App.png)  


## Features
- View and copy colors in different formats
- Select exact pixel in `Zoomview` with mouse or keyboard post-capture
- Global hotkey to start/stop capture
- Window always-on-top toggle
- Intuitive, easy-to-use UI _(I hope...)_
- Persistent user settings


## Screenshots
  
![Settings](/Images/Screenshots/AppSettingsPage.png)  

![Format menu ](/Images/Screenshots/AppMenu.png)  


## Getting Started

#### Requirements:
- Windows
- [.NET 9.0](https://dotnet.microsoft.com/en-us/download/dotnet) _(if running locally)_    

<!-- 
#### Microsoft Store
- Link here TODO
-->
  
#### Download .exe
- Download the latest release from the [Releases](https://github.com/Peppson/color-picker/releases) page  

#### Build from source
```bash
git clone "https://github.com/Peppson/color-grab.git"
cd "color-grab/colorPicker"
dotnet publish -c Release -r win-x64 --self-contained true -o "$HOME/Desktop"
```

#### Run locally
```bash
git clone "https://github.com/Peppson/color-grab.git"
cd "color-grab/colorPicker"
dotnet run -c Release 
```

### Considerations
- Auto-copy last capture to clipboard?
- Change `ZoomView` to use GPU accelareted capture?
- Keep sampling active with no mouse movement, to capture say a video?
- WPF is hard-capped at 60 FPS, such a shame...

<br>
