# Dangerous Dave in C#

This is a C# implementation of id Software's classic game, Dangerous Dave, inspired by MaiZure's C implementation and video series: https://github.com/MaiZure/lmdave/
For the Node.js version, see this repository: https://github.com/Goldsmith42/dave-nodejs

This implementation goes about as far as MaiZure's did, but the end goal is to go further and finish the implementation. This is mostly intended as a programming exercise.

See the `TODO` file for things that are on the roadmap.

## Usage

### Requirements

- .NET 8
- You have to provide the original executable of the game (all assets will be extracted from the game files).

### Building

You can build and run the project using the default launch configuration in Visual Studio Code. If the assets have not been extracted, you will need to provide a decompressed version of the original Dangerous Dave executable.

You can use UNLZEXE to decompress the original DAVE.EXE file. MaiZure's videos provide instructions on how to do this using DOSBox, but it's much simpler to use a modern version of UNLZEXE for your system.

You can specify the location of the uncompressed executable by modifying the `config.json` file at the root of this project. For the sake of convenience, you can place the file inside a folder called `original-game` and it will automatically be copied to the executable directory on build.