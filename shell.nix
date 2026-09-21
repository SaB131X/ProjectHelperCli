{ pkgs ? import <nixpkgs>{}}:

let
    dotnet = pkgs.dotnet-sdk_10;
    nativeLibs = with pkgs;[

    ];
in
pkgs.mkShell {
    nativeBuildInputs = [
        dotnet
    ] ++ nativeLibs;
    LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath nativeLibs;
    DOTNET_ROOT = "${dotnet}/share/dotnet";
    shellHook = ''
        echo "Dotnet Dev Env"
        echo "dotnet ver: $(dotnet --version)"
    '';
}
