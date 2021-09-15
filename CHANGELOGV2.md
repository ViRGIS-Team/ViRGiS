# Change Log and release notes for ViRGIS Version 2

## Version 2.1.3

Contents :

- Container Layers and support for multilayer Vector files
- Experimental Grid Object
- removal of all NuGET dependencies
- moving to UNITY-RX for events
- updated to PDAL-C 2.1.1 and removing the PLY file write step for DEM Layers
- Introduction of the TIN layer
- PG Data now working
- Introdcution of multiple views

Supported on :

- Windows - x64 Desktop mode
- Windows - Rift VR mode
- Windows - Steam VR
- MacOS - Desktop mode
- Linux - Desktop Mode

Built on

- Unity 2021.1.20
- GDAL 3.3.1
- PDAL 2.2.0
- MDAL 0.8.1
- G3 2.0.0
- GeoJSON 1.2.17
- Unity XR IT 1.0.0
- netDXF  2.4.1

## Version 2.1.2

This version will run on the following platforms :

- Windows - x64 Desktop mode
- Windows - Rift VR mode

### Contents

Following Functions :

- updates to allow the Virgis Geo new UI
- Added PLY and 3DS #8 #9
- Added Editable meshes #7
- Added Wireframe Edit Mode for meshes
- updates GDAL to 3.2.2
- updates PDAL to 2.2.0 with async scripts #13
- updates MDAL to 0.8.0 with async scripts #12
- updates branding

GDAL Version : 3.2.2
PDAL Version : 2.2.0
MDAL Version : 0.8.0
netDXF Version : 2.4.1

## Version 2.1.1

### Platforms

This version will run on the following platforms :

- Windows - x64 Desktop mode
- Windows - Rift VR mode

### Contents

- Refactored into a UPM Package
- Removed all dependencies on MapBox and Oculus from the package and moved to the end user project

GDAL Version : 3.1.4
PDAL Version : 2.2.0
MDAL Version : 0.7.1

> After Version 2.1 ViRGiS Version 2 was converted from a complete Unity project
> to a Unity Package Manager (UPM) package.
>
> Therefore  - release numbers now refer to the Package version number and not an end-user
> Application


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
 
