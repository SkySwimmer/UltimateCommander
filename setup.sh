#!/bin/bash
missingdepends=false

echo Searching for make...
if command -v make &> /dev/null 
then
    requiredver=$requiredvermake
    currentver="$(make --version|grep --only-matching --perl-regexp "\\d+\.\\d+")"
    packagename=make
    installed=false
    
    if [ "$(printf '%s\n' "$requiredver" "$currentver" | sort -V | head -n1)" = "$requiredver" ]; then 
        echo $packagename is installed.
        installed=true
    else
        echo $packagename is installed but OUTDATED, minimal version: $requiredver, current version: $currentver
        installed=false
        missingdepends=true
    fi
    
    makeinstalled=$installed
else
    echo make is NOT installed.
    makeinstalled=false
    missingdepends=true
fi

echo Searching for Git...
if command -v git &> /dev/null 
then
    requiredver=$requiredvergit
    currentver="$(git --version|grep --only-matching --perl-regexp "\\d+\.\\d+\.\\d+")"
    packagename=Git
    installed=false
    
    if [ "$(printf '%s\n' "$requiredver" "$currentver" | sort -V | head -n1)" = "$requiredver" ]; then 
        echo $packagename is installed.
        installed=true
    else
        echo $packagename is installed but OUTDATED, minimal version: $requiredver, current version: $currentver
        installed=false
        missingdepends=true
    fi
    
    gitinstalled=$installed
else
    echo Git is NOT installed.
    makeinstalled=false
    missingdepends=true
fi

if [ $missingdepends == "true" ]; then
    echo
    echo You are missing the following dependencies:
    if [ "$dotnetinstalled" == "false" ]; then 
        echo Mono version $requiredverdotnet or above.
    fi
    if [ "$msbuildinstalled" == "false" ]; then 
        echo MSBuild version $requiredvermsbuild or above.
    fi
    if [ "$nugetinstalled" == "false" ]; then 
        echo NuGet version $requiredvernuget or above.
    fi
    if [ "$makeinstalled" == "false" ]; then 
        echo Make version $requiredvermake or above.
    fi
    if [ "$gitinstalled" == "false" ]; then 
        echo Git version $requiredvergit or above.
    fi
    exit 1
fi

function applyPatches() {
    for file in "$1"/*; do
        if [ -d "$file" ]; then
            applyPatches "$file" "$2/$(basename "$file")"
        elif [ -f "$file" ]; then
            patch -d "$2" -i "$(pwd)/$file"
        fi
    done
}

echo Cloning repositories...

echo Cloning CMD-R base...

rm -rf work
mkdir "work"
cd work

rm -rf base-project
git clone https://github.com/Stefan0436/CMD-R.git base-project

if [ -d "../patches/base" ]; then
    echo "Patching CMD-R...";
    applyPatches "../patches/base" "base-project"
fi

echo Building CMD-R base project...
cd base-project
chmod +x configure
./configure --norepoconfig || exit 1
make
echo
cp -rf build ../build
cd ..

echo Cloning module projects...
for module in modules/*; do
    if [ "$module" == "modules/*" ]; then
        break
    fi
    source "$module"
    echo "Downloading $module..."
    
    rm -rf "module-projects/$module"
    git clone "$url" "module-projects/$module"
    
    if [ -d "../patches/$module" ]; then
        echo "Patching $module...";
        applyPatches "../patches/$module" "module-projects/$module"
    fi
    
    cd "module-projects/$module"
    echo "Building $module..."
    chmod +x configure
    ./configure --norepoconfig || exit 1
    make
    make package
    echo
    mkdir -p "../../build/Module Packages"
    cp "build/package.cpkg" "../../build/Module Packages"
    cd ../..
done

echo Installing CMD-R to the SDK...
cd ..
if [ ! -d sdk/libraries ]; then
    mkdir -p sdk/libraries
fi
if [ ! -d sdk/run ]; then
    mkdir -p sdk/run
fi
cp -vrf work/build/. sdk/run
cp -vf work/build/CMD-R.dll sdk/libraries
echo Done.
