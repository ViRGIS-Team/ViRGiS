@echo off

SET prefix=D:/bld/pdal_1599683721292/_h_env/Library
SET exec_prefix=D:/bld/pdal_1599683721292/_h_env/Library/bin
SET libdir=D:/bld/pdal_1599683721292/_h_env/Library/lib


IF "%1" == "--libs" echo -LD:/bld/pdal_1599683721292/_h_env/Library/lib -lpdalcpp & goto exit
IF "%1" == "--plugin-dir" echo D:/bld/pdal_1599683721292/_h_env/Library/bin & goto exit
IF "%1" == "--prefix" echo %prefix% & goto exit
IF "%1" == "--ldflags" echo -L%libdir% & goto exit
IF "%1" == "--defines" echo  & goto exit
IF "%1" == "--includes" echo -ID:/bld/pdal_1599683721292/_h_env/Library/include -ID:/bld/pdal_1599683721292/_h_env/Library/include -ID:/bld/pdal_1599683721292/_h_env/Library/include -ID:/bld/pdal_1599683721292/_h_env/Library/include & goto exit
IF "%1" == "--cflags" echo /DWIN32 /D_WINDOWS /W3 & goto exit
IF "%1" == "--cxxflags" echo /DWIN32 /D_WINDOWS /W3 /GR /EHsc -std=c++11 & goto exit
IF "%1" == "--version" echo 2.2.0 & goto exit


echo Usage: pdal-config [OPTIONS]
echo Options:
echo    [--cflags]
echo    [--cxxflags]
echo    [--defines]
echo    [--includes]
echo    [--libs]
echo    [--plugin-dir]
echo    [--version]

:exit
