#!/usr/bin/env bash

SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )

###########################################################################
# LOAD versions from build.config
###########################################################################

source $SCRIPT_DIR/build.config
if [ "$CAKE_VERSION" = "" ]; then
    echo "Failed to parse Cake version"
    exit 1
fi
if [ "$GITVERSION_VERSION" = "" ]; then
    echo "Failed to parse GitVersion version"
    exit 1
fi

###########################################################################
# INSTALL .NET Core 3.x tools
###########################################################################

TOOLS_DIR="$SCRIPT_DIR/tools"
if [ ! -d "$TOOLS_DIR" ]; then
  mkdir "$TOOLS_DIR"
fi

function install_tool () {
    packageId=$1
    toolCommand=$2
    version=$3

    toolPath="$TOOLS_DIR/.store/${packageId}/$CAKE_VERSION"
    exePath="$TOOLS_DIR/${toolCommand}"

    if [ ! -d "$toolPathExists" || ! -d "$exePath" ]; then

        if [ -d "$exePath" ]; then
            dotnet tool uninstall --tool-path $TOOLS_DIR $packageId
        fi

        dotnet tool install --tool-path $TOOLS_DIR --version $version --configfile NuGet.public.config $packageId
        if [ $? -ne 0 ]; then
            echo "Failed to install ${packageId}"
            exit 1
        fi
    fi

    echo $exePath
}

# We use lower cased package ids, because toLower is not defined in bash
CAKE_EXE=$(install_tool 'cake.tool' 'dotnet-cake' $CakeVersion)
install_tool 'gitversion.tool' 'dotnet-gitversion' $GitVersionVersion > /dev/null 2>&1

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

(exec "$CAKE_EXE" build.cake --bootstrap) && (exec "$CAKE_EXE" build.cake "$@")
