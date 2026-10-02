# Third-Party Notices

This repository's own source code is licensed under the MIT License (see `LICENSE`).
Redistributions of this software in binary form include the third-party components
listed below, each under its own license. Full license texts are included in the
`LICENSES/` folder.

## Apache License 2.0

### OpenCvSharp 5

- **NuGet packages:** `OpenCvSharp5.Windows`, `OpenCvSharp5.GdipExtensions`, `OpenCvSharp5.WpfExtensions`
- **Copyright:** © shimat and the OpenCvSharp contributors
- **License:** Apache License 2.0 — full text in `LICENSES/Apache-2.0.txt`
- **Source:** https://github.com/shimat/opencvsharp

### OpenCV native runtime

- **Shipped as:** native binaries bundled by `OpenCvSharp5.Windows` (e.g. `OpenCvSharpExtern.dll` and the OpenCV libraries it links)
- **Copyright:** © the OpenCV team and contributors
- **License:** Apache License 2.0 — full text in `LICENSES/Apache-2.0.txt`
- **Source:** https://github.com/opencv/opencv

## MIT License

License text: `LICENSES/MIT.txt`.

### Microsoft.Xaml.Behaviors.Wpf

- **NuGet package:** `Microsoft.Xaml.Behaviors.Wpf`
- **Copyright:** © Microsoft Corporation
- **License:** MIT
- **Source:** https://github.com/microsoft/XamlBehaviorsWpf

### ggml native libraries

- **Shipped as:** `Lib/ggml.dll`, `Lib/ggml-base.dll`, `Lib/ggml-cpu.dll`
- **Copyright:** © Georgi Gerganov and the ggml.ai contributors
- **License:** MIT
- **Source:** https://github.com/ggml-org/ggml

### sam3.cpp (and ggml)
- License: MIT
- Original author: Pierre-Antoine Bannier (PABannier)
- Repository: https://github.com/PABannier/sam3.cpp
  (and/or https://github.com/rwmcclellan/sam3.cpp)
- Notes: Native inference engine used via SamNet.Native

### .NET and WPF runtime

- **Shipped as:** Microsoft .NET runtime and Windows Desktop (WPF) framework, `System.Drawing.Common`
- **Copyright:** © Microsoft Corporation
- **License:** MIT
- **Source:** https://github.com/dotnet

### Segment Anything model weights

- **SAM 2 / SAM 2.1** – Apache License 2.0  
  Original models by Meta AI (FAIR).  
  See the official SAM 2 repository for full license terms.

- **SAM 3** – SAM License (Meta)  
  Original models by Meta AI.  
  Redistribution and use of the weights are subject to Meta’s SAM License.  
  Users should review the license that accompanies any model files they download.

This project does not redistribute the model weight files. Users obtain them
from the official sources (or the sam3.cpp model zoo conversions).

## Distribution history

Versions of this project distributed before the OpenCvSharp migration included
**Emgu CV** (GPL-3.0, or a commercial license) and were distributed under the
GPL-3.0 license for the project as a whole. Those historical versions remain
available under GPL-3.0; current and future versions no longer depend on Emgu CV
and are licensed under MIT.
