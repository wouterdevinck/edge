#!/bin/bash
set -e

CONFIG=edgeos_x86_64_defconfig

BR_VERSION=2020.08-rc1
# Note: the iotedge package depends on Buildroot 2020.08-rc1 or higher because of rust/cargo updates after 2020.05.

SCRIPT_DIR="$( cd "$( dirname "$0" )" && pwd )"
WORKDIR="$SCRIPT_DIR/buildroot"
EXTDIR="$SCRIPT_DIR/external"
OUTDIR="$SCRIPT_DIR/output"

CONFPATH="$EXTDIR/configs/$CONFIG"

function printUsage {
  echo "Usage: $0 prepare|menuconfig|build|clean"
  echo ""
  echo "   prepare    - Download buildroot and unpack into working directory."
  echo "   menuconfig - Run the menuconfig utility."
  echo "   build      - Build the image."
  echo "   clean      - Clean up."
  echo ""
}

case $1 in

"prepare")
  rm -rf $WORKDIR
  mkdir -p $WORKDIR
  wget "https://buildroot.org/downloads/buildroot-$BR_VERSION.tar.gz"
  tar -xzf buildroot-$BR_VERSION.tar.gz -C $WORKDIR --strip-components 1
  rm buildroot-$BR_VERSION.tar.gz
  ;;

"build"|"menuconfig")
  if [ ! -d "$WORKDIR" ]; then
    printf "\nPlease run prepare first.\n\n"
    exit 2
  fi
  cd $WORKDIR
  ;;&

"menuconfig")
  make O=$OUTDIR BR2_DEFCONFIG=$CONFPATH  defconfig
  make O=$OUTDIR BR2_EXTERNAL=$EXTDIR     menuconfig
  make O=$OUTDIR BR2_DEFCONFIG=$CONFPATH  savedefconfig
  ;;

"build")
  make O=$OUTDIR BR2_EXTERNAL=$EXTDIR $CONFIG
  make O=$OUTDIR
  qemu-img convert -f raw $OUTDIR/images/disk.img -O vhdx -o subformat=dynamic $OUTDIR/images/disk.vhdx
  ;;

"clean")
  rm -rf $WORKDIR $OUTDIR
  ;;

*)
  printUsage
  ;;

esac
