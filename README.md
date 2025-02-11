# DOTNET fork of: 1️⃣🐝🏎️ The One Billion Row Challenge

This is a fork of the [original Java version](https://github.com/gunnarmorling/1brc) of the challenge, adapted for .NET. Very early on in the process but I wanted to see how fast .NET could be. Shout out to this post for the inspiration: https://twitter.com/KooKiz/status/1742168051724775559

## Orignal Description:

The One Billion Row Challenge (1BRC) is a fun exploration of how far modern Java can be pushed for aggregating one billion rows from a text file.
Grab all your (virtual) threads, reach out to SIMD, optimize your GC, or pull any other trick, and create the fastest implementation for solving this task!

<img src="1brc.png" alt="1BRC" style="display: block; margin-left: auto; margin-right: auto; margin-bottom:1em; width: 50%;">

The text file contains temperature values for a range of weather stations.
Each row is one measurement in the format `<string: station name>;<double: measurement>`, with the measurement value having exactly one fractional digit.
The following shows ten rows as an example:

```
Hamburg;12.0
Bulawayo;8.9
Palembang;38.8
St. John's;15.2
Cracow;12.6
Bridgetown;26.9
Istanbul;6.2
Roseau;34.4
Conakry;31.2
Istanbul;23.0
```

The task is to write a Java program which reads the file, calculates the min, mean, and max temperature value per weather station, and emits the results on stdout like this
(i.e. sorted alphabetically by station name, and the result values per station in the format `<min>/<mean>/<max>`, rounded to one fractional digit):

```
{Abha=-23.0/18.0/59.2, Abidjan=-16.2/26.0/67.3, Abéché=-10.0/29.4/69.0, Accra=-10.1/26.4/66.4, Addis Ababa=-23.7/16.0/67.0, Adelaide=-27.8/17.3/58.5, ...}
```
 

## Prerequisites

[Java 21](https://openjdk.org/projects/jdk/21/) must be installed on your system.

[Dotnet 8](https://dotnet.microsoft.com/download/dotnet/8.0) must be installed on your system.

## Running the Dotnet Implementation

The dotnet version uses the same program to generate the test data and uses a similar command to run.

The dotnet implementation is stored under `/src/main/csharp`. The core file is `CalculateAverage.cs`. 

This repository contains two programs from the source repo:

* `dev.morling.onebrc.CreateMeasurements` (invoked via _create\_measurements.sh_): Creates the file _measurements.txt_ in the root directory of this project with a configurable number of random measurement values
* `dev.morling.onebrc.CalculateAverage` (invoked via _calculate\_average.sh_): Calculates the average values for the file _measurements.txt_

Execute the following steps to run the challenge:

1. Build the project using Apache Maven:

    ```
    ./mvnw clean verify
    ```

2. Create the measurements file with 1B rows (just once):

    ```
    ./create_measurements.sh 1000000000
    ```

    This will take a few minutes.
    **Attention:** the generated file has a size of approx. **12 GB**, so make sure to have enough diskspace.

3. Calculate the average measurement values:

    ```
    ./calculate_average_csharp.sh
    ```

    I have two implementations so far, a simple implementation that reads in all the lines of text, and a faster implementation that uses Parallel.ForEach and ConcurrentDictionary to process the file in parallel.

## Limitations/TODO

* Only setup to run on Linux.
* More optimizations can be made


## What I've tried so far

* Async
* Parallel.ForEach
* SIMD
* SIMD + Parallel.ForEach
* Stackalloc
* Spans

## Results

For now, I am only running comparisons on my machine under WSL2. All tests were run on the same machine. So I would pay attention more to performanece relative to the Java versions rather than raw times. Here are the results:

| Implementation  | Time (m:s:ms) |
| --------------- | ------------- |
| Java (Baseline) | 1:34:00       |
| C# (Baseline)   | DNF           |
| C# Parallel     | ~0:43:00      |
| C# SIMD         | ~0:53.00      |


By my testing, so far, not using SIMD and using Parallel.ForEach is the fastest implementation. 

## Rules from the Java version:

The following rules and limits apply:

* Any of these Java distributions may be used:
    * Any builds provided by [SDKMan](https://sdkman.io/jdks)
    * Early access builds available on openjdk.net may be used (including EA builds for OpenJDK projects like Valhalla)
    * Builds on [builds.shipilev.net](https://builds.shipilev.net/openjdk-jdk-lilliput/)
If you want to use a build not available via these channels, reach out to discuss whether it can be considered.
* No external library dependencies may be used
* Implementations must be provided as a single source file
* The computation must happen at application _runtime_, i.e. you cannot process the measurements file at _build time_
(for instance, when using GraalVM) and just bake the result into the binary

## License

This code base is available under the Apache License, version 2.
