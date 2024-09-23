# Enum to class generator

### Version management

Add git tag with new version number:

`git tag v1.0.3`

If you are building packages on github, then push the tag:
`git push --tags`

Build using release configuration:

`dotnet build -c Release`

The build command will also generate nuget package with the version eqal to the tag.


## Remove stale tags

`git fetch --prune --tags`