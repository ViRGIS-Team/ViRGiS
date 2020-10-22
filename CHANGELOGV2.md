# Change Log and release notes for ViRGIS Version 2

## Version 2.1.1RC1

### Platforms

This version will run on the following platforms :

- Windows - x64 Desktop mode
- Windows - Rift VR mode

### Contents

- External Dependencies moved to UPM Packages
- Added .3ds support
- Fixed .dxf support

GDAL Version : 3.1.2
PDAL Version : 2.2.0
MDAL Version : 0.7.1


## Version 2.0.0a3

### Platforms

This version will run on the following platforms :

- Windows - x64 Desktop mode
- Windows - Rift VR mode


### Contents

- #230 - additional fixes for the "click"issue
- #83 - added tooltips on the controllers
- #70 - added metadata query tool
- #162 - use the metatdata query to show coordinates during add
- standardised polygon draw functions and UV creation functions across all polygon layer types and added mesh collider to make metadata work 
- improved the polygon tiling functions
- synched the Projection defintion to the Virgis Backend project.
- added Project version dependence
- imporved support for WFS, WMS, WCS and WMTS

GDAL Version : 3.1.2
PDAL Version : 2.2.0
MDAL Version : 0.7.0

## Version 2.0.0a2

### Platforms

This version will run on the following platforms :

- Windows - x64 Desktop mode
- Windows - Rift VR mode


### Contents

- #11 - Add GDAL integration and use OGR to load all Vector datasets
- As part of this - Add Raster Datasets ingested as Point Clouds through PDAL using a GDAL driver
- #100 - Add PDAL as DAL for Point Cloud datasets to improve import of PLy and other file formats
- #100 - Add MDAL as DAL for Mesh data sets (MDAL Layers) in addition to G3 to allow ingestion of PLY, TIN, ESRI TIN and other file formats as data-ful meshes.
- #14 - Add CRS capabilities using OSR through GDAL. Add proj4 strings in Recordset definitions.
- Create a Transverse mercator custom projection for ViRGIS. Create a Projpipeline for the coordinate axis swap. reproject all data to this projection to get the Unity Game coordinates (in Map Local space).
- #80 - Create a Hierachy of Layers with the "Container Layer" defintition. Make Mapbox a Container Layer and removed from the Map root - so that it is possible to have a model without a map.
- Converted all Vector layers to OGR layers with one layer  type "Vector". the layer is hierarchical - with sub layers for each layer type etc.
- Added DEM as a layer type that will accept any GDAL, PDAL or MDAL input type and attempt to create a DEM Mesh.
- #71 Added the file menu and tideied up the Interface (also #200)

GDAL Version : 3.1.2
PDAL Version : 2.1.0
MDAL Version : 0.6.93
 
