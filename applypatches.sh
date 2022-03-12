#!/bin/bash
if [ ! -d work ]; then
    1>&2 echo Nothing to do, please run configure first
    exit 1
fi

cd work
function applyPatches() {
    for file in "$1"/*; do
        if [ -d "$file" ]; then
            applyPatches "$file" "$2/$(basename "$file")"
        elif [ -f "$file" ]; then
            if [ ! -d "$2" ]; then
                mkdir -p "$2"
            fi
            patch -d "$2" -i "$(pwd)/$file"
        fi
    done
}

if [ -d "../patches/base" ]; then
    echo "Patching CMD-R...";
    applyPatches "../patches/base" "base-project"
fi

for module in ../modules/*; do
    if [ "$module" == "../modules/*" ]; then
        break
    fi
    if [ -d "../patches/$module" ]; then
        echo "Patching $module...";
        applyPatches "../patches/$module" "module-projects/$module"
    fi
done

cd ..
