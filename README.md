# Package Dependency Reporter
Command line utility for scanning .csproj project package dependencies and providing errors or warnings based on inconsistencies with used versions or target frameworks. For example a solution that has various projects using version 1.0, 1.1, and 1.2 of some nuget package may not deploy with all 3 versions and result in unexpected or undesired behavior due to the mismatches.


## Getting Started



### Packages

```
CommonLineArgumentsParser
Costura.Fody
```

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/your/project/tags). 

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details
