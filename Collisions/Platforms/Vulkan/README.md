# Collisions (Vulkan)

## Building and Running

### DesktopVK (Cross-platform)

#### Before Building

* Goto https://github.com/MonoGame/MonoGame/actions and find the latest run that was built the `develop` branch.
* Download the binary artefact, which will have a name with a similar format to `MonoGame.3.8.*4*.*-develop`
* Extract that directory as a top level folder of these samples. eg. if you downloaded `MonoGame.3.8.4.2908-develop` the not level folder name should be `MonoGame.3.8.4.2908-develop` and within that will the extracted contents of you artefact your downloaded.
* Currently the VK csproj looks for `MonoGame.3.8.4.2908-develop`, so you may need to edit the *VK.csproj to point to which ever artefact you downloaded and unzipped.

#### Building and Running
If the above went well, execute the following command in the top level directory of this sample.

```
dotnet run --project Platforms/Vulkan/CollisionSample.DesktopVK.csproj
```


