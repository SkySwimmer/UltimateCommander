#!/bin/bash

pwd="$(pwd)"
echo Building patches...
rm -rf "$pwd/patches"

function buildPatches()  {
    echo "Building $1 patches..."
    mkdir -p "$pwd/patches/$1"
    git config core.fileMode false
    git add -A
    buildDir "" "$(realpath "$pwd/patches/$1")"
}

function buildDir() {
    for item in *; do
         if [ -d "$item" ]; then
            cd "$item"
            buildDir "$1$item/" "$2"
            cd ..
         elif [ -f "$item" ]; then
            patch="$(git diff HEAD "$item")"
            if [ "$patch" == "" ]; then
                continue
            fi
            if [ ! -d "$2/$1" ]; then
                mkdir -p "$2/$1"
            fi
            echo "$patch" > "$2/$1$item.patch"
         fi
    done
}

cd work/base-project
buildPatches base 
cd ../..

for module in modules/*; do
    if [ "$module" == "modules/*" ]; then
        break
    fi
    source "$module"
    cd "work/module-projects/$module"
    buildPatches "$module"
    cd "$pwd"
done
