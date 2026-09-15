# Third-Party Components

SamNet is licensed under the GNU General Public License v3.0.

It includes or depends on the following third-party components:

## SamNet.Native
- License: MIT
- Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
- Repository: https://github.com/rwmcclellan/SamNet.Native
- Notes: Included in this repository for convenience. The MIT license
  continues to apply to the SamNet.Native code itself.

## sam3.cpp
- License: MIT
- Original author: Pierre-Antoine Bannier (PABannier)
- Repository: https://github.com/PABannier/sam3.cpp
  (and/or https://github.com/rwmcclellan/sam3.cpp)
- Notes: Native inference engine used via SamNet.Native.

## ggml
 — MIT, Georgi Gerganov / ggml.ai (via PABannier's fork)

## Emgu.CV
- License: GPLv3 (open-source edition)
- Website: https://www.emgu.com
- Notes: Used for image loading, mask visualization, and post-processing.
  Because Emgu.CV is GPLv3, the combined SamNet application is distributed
  under GPLv3.

## Segment Anything model weights

- **SAM 2 / SAM 2.1** – Apache License 2.0  
  Original models by Meta AI (FAIR).  
  See the official SAM 2 repository for full license terms.

- **SAM 3** – SAM License (Meta)  
  Original models by Meta AI.  
  Redistribution and use of the weights are subject to Meta’s SAM License.  
  Users should review the license that accompanies any model files they download.

This project does not redistribute the model weight files. Users obtain them
from the official sources (or the sam3.cpp model zoo conversions).

---

The MIT-licensed components remain under MIT.  
The overall SamNet application is distributed under GPLv3.
