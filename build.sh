#!/bin/bash
if [ ! -d work ]; then
    1>&2 echo Nothing to do, please run configure first
    exit 1
fi

cd work
echo Building CMD-R base project...
cd base-project
chmod +x configure
./configure --norepoconfig || exit 1
make
echo
rm -rf ../build
cp -rf build ../build
cd ..

echo Building module projects...
for module in ../modules/*; do
    if [ "$module" == "../modules/*" ]; then
        break
    fi
    
    olddir="$(pwd)"
    
    subdir=""
    source "$module"
    if [ ! -d "module-projects/$module" ]; then
        continue
    fi
    
    mkdir -p "build/Module Packages"
    cd "module-projects/$module"
    
    if [ "$subdir" != "" ]; then
        cd "$subdir"
    fi
    
    echo "Building $module..."
    chmod +x configure
    ./configure --norepoconfig || exit 1
    make
    make package
    echo
    
    cp "build/package.cpkg" "$olddir/build/Module Packages/$module.cpkg"
    cd "$olddir"
done

echo Building local module projects...
for module in ../local-modules/*; do
    if [ "$module" == "../local-modules/*" ]; then
        break
    fi
    
    olddir="$(pwd)"
    
    subdir=""
    source "$module/settings.module.conf"
    if [ ! -d "module-projects/$module" ]; then
        continue
    fi
    
    mkdir -p "build/Module Packages"
    cd "module-projects/$module"
    
    if [ "$subdir" != "" ]; then
        cd "$subdir"
    fi
    
    echo "Building $module..."
    chmod +x configure
    ./configure --norepoconfig || exit 1
    make
    make package
    echo
    
    cp "build/package.cpkg" "$olddir/build/Module Packages/$module.cpkg"
    cd "$olddir"
done

echo Building the CMDR.DM library...
cd ..
cd CMDR.DM
dotnet build
cd ..

echo Installing CMD-R to the SDK...
if [ ! -d sdk/libraries ]; then
    mkdir -p sdk/libraries
fi
if [ ! -d sdk/run ]; then
    mkdir -p sdk/run
fi
cp -vrf work/build/. sdk/run
cp -vf work/build/CMD-R.dll sdk/libraries

echo Installing CMDR.DM to the SDK...
cp -vf CMDR.DM/bin/Debug/net7.0/CMDR.DM.dll sdk/libraries
echo Done.
