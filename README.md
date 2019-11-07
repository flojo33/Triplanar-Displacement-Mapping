
# Triplanar-Displacement-Mapping

TUM Masters Thesis - Triplanar Displacement Mapping for Terrain Rendering - Florian Bayer

![Title Image](http://florian.diebayers.de/master/title.png)

# Abstract

Height mapping is a common technique for creating terrains in games and simulations. While being very simple and fast to implement, the method lacks possibilities to create nice details along steep cliffs and edges. This thesis proposes a combination of triplanar mapping, displacement mapping, and tessellation to create extruded and indented geometry along strongly displaced faces of height-based terrains.
The biggest downsides of triplanar displacement mapping are collisions and performance, so possible solutions to these problems are discussed and evaluated. A distance based level-of-detail function is introduced to reduce details far away from the camera. Additionally a dynamic tessellation map is proposed to reduce the amount of created geometry in flat areas of the terrain. Finally, a dynamic collision mesh generator was introduced to create tessellated physics colliders on the fly. This is achieved by replicating a similar tessellation algorithm on the processor and utilizing General Purpose Computation on Graphics Processing Units (GPGPU) to displace the tessellated collision meshes using the same process as the main terrain shader.

# Kurzfassung

Height-Mapping ist die am häufigsten verwendete Methode, um in Spielen oder Simulationen Landschaften einfach und schnell zu generieren. Allerdings bietet sie nur wenige Möglichkeiten Details entlang steiler Klippen und Bergseiten zu erstellen. Diese Arbeit stellt eine Kombination aus Triplanarem Mapping, Displacement Mapping und Tesselierung vor, die es ermöglicht in diesen Bereichen mehr Geometrie hinzuzufügen. Die größten Probleme von Triplanarem Displacement Mapping stellen die benötigte Rechenleistung, sowie Kollisionen dar. Einige Lösungsansätze zu diesen Problemen, wie zum Beispiel distanzbasiertes Ein- und Ausblenden von Details, dynamische Tesselierungsmaps und Echtzeit-tesselierte Kollisionsgeometrie werden präsentiert und evaluiert.

# Thesis

The final thesis can be downloaded [here](https://www.dropbox.com/s/70h3wlgfnekm35o/Florian%20Bayer%20-%20Triplanar%20Displacement%20Mapping%20for%20Terrain%20Rendering.pdf?dl=0).

# Demo Download

The built demos for mac and windows can be found here:
- [Windows](https://www.dropbox.com/s/92mu31fm14839cn/Demo%20Windows.zip?dl=0)
- [Mac](https://www.dropbox.com/s/92mu31fm14839cn/Demo%20Windows.zip?dl=0)

# Unity Project

This demo Project was implemented in Unity 3D. To open the source of the project simply pull and open the main scene inside the Unity editor. The project was tested for Windows and Mac. Mobile platforms are currently not supported because tessellation and other high-level shader features are not available there.
