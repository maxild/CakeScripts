#!/usr/bin/env bash

SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )

###########################################################################
# LOAD versions from build.config
###########################################################################

trim() {
  local var="$*"
  # remove leading whitespace characters
  var="${var#"${var%%[![:space:]]*}"}"
  # remove trailing whitespace characters
  var="${var%"${var##*[![:space:]]}"}"
  printf '%s' "$var"
}

source $SCRIPT_DIR/build.config
# For some reason we have to remove whitespace
DOTNET_VERSION=$(trim "$DOTNET_VERSION")
CAKE_VERSION=$(trim "$CAKE_VERSION")
GITVERSION_VERSION=$(trim "$GITVERSION_VERSION")

if [ "$DOTNET_VERSION" = "" ]; then
    echo "Failed to parse .NET Core SDK version"
    exit 1
fi
if [ "$CAKE_VERSION" = "" ]; then
  echo "Failed to parse Cake version"
  exit 1
fi
if [ "$GITVERSION_VERSION" = "" ]; then
  echo "Failed to parse GitVersion version"
  exit 1
fi

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
export DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX=2

DOTNET_INSTALLED_VERSION=$(dotnet --version)
if [ $? -ne 0 ]; then
  # Extract the first line of the message without making bash write any error messages
  echo "$DOTNET_INSTALLED_VERSION" | head -1
  echo "That is not problem, we will install the SDK version below."
  DOTNET_INSTALLED_VERSION='' # Force installation of .NET Core SDK via dotnet-install script
else
  echo ".NET Core SDK version ${DOTNET_INSTALLED_VERSION} found."
fi

if [[ "$DOTNET_VERSION" != "$DOTNET_INSTALLED_VERSION" ]]; then
  echo "Installing .NET Core SDK version ${DOTNET_VERSION} ..."
  if [ ! -d "$SCRIPT_DIR/.dotnet" ]; then
    mkdir "$SCRIPT_DIR/.dotnet"
  fi
  curl -Lsfo "$SCRIPT_DIR/.dotnet/dotnet-install.sh" https://dot.net/v1/dotnet-install.sh > /dev/null 2>&1
  bash "$SCRIPT_DIR/.dotnet/dotnet-install.sh" --version $DOTNET_VERSION --install-dir .dotnet --no-path > /dev/null 2>&1
  # Note: This PATH/DOTNET_ROOT will be visible only when sourcing script.
  # Note: But on travis CI or other *nix build machines the PATH does not have
  #       to be visible on the commandline after the build
  export PATH="$SCRIPT_DIR/.dotnet:$PATH"
  export DOTNET_ROOT="$SCRIPT_DIR/.dotnet"
fi

###########################################################################
# INSTALL .NET Core 3.x tools
###########################################################################

TOOLS_DIR="$SCRIPT_DIR/tools"
if [ ! -d "$TOOLS_DIR" ]; then
  mkdir "$TOOLS_DIR"
fi

function install_tool () {
  local packageId=$1
  local toolCommand=$2
  local version=$3

  toolPath="$TOOLS_DIR/.store/${packageId}/${version}"
  exePath="$TOOLS_DIR/${toolCommand}"

  if [ ! -d "$toolPath" ] || [ ! -f "$exePath" ]; then

    if [ -f "$exePath" ]; then
      dotnet tool uninstall --tool-path $TOOLS_DIR $packageId > /dev/null 2>&1
    fi

    dotnet tool install --tool-path $TOOLS_DIR --version $version --configfile NuGet.public.config $packageId > /dev/null 2>&1
    if [ $? -ne 0 ]; then
      echo "Failed to install ${packageId}"
      exit 1
    fi

  fi

  echo $exePath
}

# We use lower cased package ids, because toLower is not defined in bash
CAKE_EXE=$(install_tool 'cake.tool' 'dotnet-cake' $CAKE_VERSION 2>&1)
install_tool 'gitversion.tool' 'dotnet-gitversion' $GITVERSION_VERSION > /dev/null 2>&1

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

echo "Running build script..."
(exec "$CAKE_EXE" build.cake --bootstrap) && (exec "$CAKE_EXE" build.cake "$@")
