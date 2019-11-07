
# Triplanar-Displacement-Mapping

TUM Masters Thesis - Triplanar Displacement Mapping for Terrain Rendering - Florian Bayer

![Title Image](http://florian.diebayers.de/master/title.png)

# Abstract

Height mapping is a common technique for creating terrains in games and simulations. While being very simple and fast to implement, the method lacks possibilities to create nice details along steep cliffs and edges. This thesis proposes a combination of triplanar mapping, displacement mapping, and tessellation to create extruded and indented geometry along strongly displaced faces of height-based terrains.
The biggest downsides of triplanar displacement mapping are collisions and performance, so possible solutions to these problems are discussed and evaluated. A distance based level-of-detail function is introduced to reduce details far away from the camera. Additionally a dynamic tessellation map is proposed to reduce the amount of created geometry in flat areas of the terrain. Finally, a dynamic collision mesh generator was introduced to create tessellated physics colliders on the fly. This is achieved by replicating a similar tessellation algorithm on the processor and utilizing General Purpose Computation on Graphics Processing Units (GPGPU) to displace the tessellated collision meshes using the same process as the main terrain shader.

# Kurzfassung

Height-Mapping ist die am häufigsten verwendete Methode, um in Spielen oder Simulationen Landschaften einfach und schnell zu generieren. Allerdings bietet sie nur wenige Möglichkeiten Details entlang steiler Klippen und Bergseiten zu erstellen. Diese Arbeit stellt eine Kombination aus Triplanarem Mapping, Displacement Mapping und Tesselierung vor, die es ermöglicht in diesen Bereichen mehr Geometrie hinzuzufügen. Die größten Probleme von Triplanarem Displacement Mapping stellen die benötigte Rechenleistung, sowie Kollisionen dar. Einige Lösungsansätze zu diesen Problemen, wie zum Beispiel distanzbasiertes Ein- und Ausblenden von Details, dynamische Tesselierungsmaps und Echtzeit-tesselierte Kollisionsgeometrie werden präsentiert und evaluiert.
