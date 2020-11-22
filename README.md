# ViRGIS Project


## Virtual Reality GIS in Unity


- [ViRGIS Project](#virgis-project)
- [Overview](#overview)
- [Documentation](#documentation)
- [Getting ViRGIS](#getting-virgis)
- [Contributing](#contributing)
- [License](#license)

![Virgis Screen Shot](https://www.virgis.org/images/virgis_landscape.png)

# Overview

ViRGIS (**Vi**rtual **R**eality **G**eospatial **I**nformation **S**ystem) is intended to bring the GIS world into the VR world.

In particular, it is intended to provide ALL of these three things :



1. The ability to load a 3D representation of a Georeferenced GIS model in a Virtual Reality world with the ability to move around the representation in an intuitive way,
2. The ability to create entities in the representation at run-time from data located outside of the VR engine - i.e. from the “GIS world’ and using GIS formats, and
3. The ability to add and modify entities in the representation from within the VR world in an intuitive way and for those changes to reflect seamlessly back into the GIS World.

Many of these features are currently available in various packages and initiatives currently. As far as we know, no one has implemented all three together and dedicated to 3D GIS. This is the USP of this product.


# Documentation

Go to [Documentation](https://www.virgis.org/v2)

# Getting ViRGIS

ViRGiS Version is provided as a [Unity Package Manager](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@2.0/manual/index.html) (UPM) package.

To install in a Unity Project, add the following lini into the `manifest.json` file: 
```
    "com.virgis.project_ns": "https://github.com/ViRGIS-Team/Project-Namespace.git",
    "com.virgis.virgis_v2": "https://github.com/ViRGIS-Team/ViRGiS_v2.git",
```

For more details on creating a Unity Project from ViRGiS - see the [documentation](https://www.virgis.org/packaging).

# License

ViRGIS is copyright [Runette Software Ltd](https://runette.co.uk) and it licensed under an MIT license (see [license](/LICENSE))

# Contributing

We actively welcome and encourage contributions.

For more details, see the [documentation](https://www.virgis.org/contributing).
