# Other 3rd Party Software

ViRGiS uses the following additional software:

- Geometry3Sharp is used for Geometric functions, especially mesh manipulation. This package includes the DMesh3 class that is the main feature container for Mesh objects in ViRGiS.
- GeoJSON.Net is used to serialize and deserialize geometries in the Project.Json configuration files.
- Delaunator - sharp (which is a C# port of the [MapBox JS Delaunator project](https://github.com/mapbox/delaunator)) is used to convert Points to Meshes - primarily creating a Mesh from a Vector geometry.