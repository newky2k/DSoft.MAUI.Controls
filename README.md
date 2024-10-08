# DSoft.MAUI.Controls

This packages currently just contains `PanPinchContainer` by [CodingOctocat](https://github.com/CodingOctocat/MauiPanPinchContainer), but more items will be added as I migrate over my Xamarin controls and extensions

Both views are 100% MAUI and do not rely on platform implementations.

 - `PanPinchContainer`
    - Zoom in and out and pan around an image 

 ###### This library has been tested on iOS and Android, but should also work on other platforms

 **NuGet**

|Name|Info|
| ------------------- | ------------------- | 
|DSoft.MAUI.PinchPanZoomImage|[![NuGet](https://buildstats.info/nuget/DSoft.MAUI.PinchPanZoomImage)](https://www.nuget.org/packages/DSoft.MAUI.PinchPanZoomImage)|

# Usage

Add the package from Nuget, either from NuGet Package Manager or command line

        dotnet add package DSoft.MAUI.PinchPanZoomImage

PinchPanZoomImage is a MAUI library so requires no dependency injection calls to initialise.  You can jump straight in.
 
## PinchZoom View

### Usage

You will need to add a namespace reference to your xaml file

```csharp
  xmlns:pinch="clr-namespace:DSoft.MAUI.PinchPanZoomImage;assembly=DSoft.MAUI.PinchPanZoomImage"  
```

## Why MauiPanPinchContainer?
I recently developed a MAUI app and needed a control that would allow the user to view an Image, like an Android/iOS photo album, I tried [Bertuzzi.MAUI.PinchZoomImage](https://github.com/TBertuzzi/Bertuzzi.MAUI.PinchZoomImage ), but it had some UX issues, then I tried reading the documentation [.NET MAUI Docs/Recognize a pangesture ](https://learn.microsoft.com/zh-cn/dotnet/maui/fundamentals/gestures/pan), [ .NET MAUI Docs/Recognize a pinch gesture](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/gestures/pinch ), After a few days of lots of attempts, I finally implemented the MauiPanPinchContainer!

Honestly, the code is all mathematical calculations, and I don't fully understand it, so if I could, I'd like to see in the code `Contnet.Anchor` to stay at the default value of 0.5 (I'm not sure if 0.5 is better), but I'm limited in my ability/time to do that for now.

## Usage
There are no plans to release a NuGet version for the time being, as PanPinchContainer is still very simple in its functionality, with hard-coded parameters, lack of flexibility, and no customizable dependency properties

```xaml
<PanPinchContainer>
    <Image Source="hello_maui.jpg" />
<PanPinchContainer>     
```

## Features
- 1x ~ 10x Scaling: Supports scaling from 1x to 10x.
- 0.5x Temporarily Scaling: Supports pinch and temporarily scale down the image below 1x. Upon release, the image restores to 1x.

### Supported
- Boundary Constraints: Limits scaling and panning within image boundaries.
- Double Tap to Zoom: Double tap to zoom in (2x) or zoom out (1x).
- Scaling Based on Pinch Position: Scale based on the position of the pinch gesture.
- Panning and Zooming Animation: Smooth panning and zooming animations.

### Not Supported
- Rotation
- Inertial panning
- Slightly more than 10x temporarily scaling

