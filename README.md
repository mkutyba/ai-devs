# AI_devs 3 assignments

## Running the code

To run the project, use launch settings from `Agent.AppHost` and provide the following environment variables:

```
Parameters:JsonCompleter-ApiKey=""
Parameters:JsonCompleter-ApiKey=""
Parameters:JsonCompleter-ReportUrl=""
Parameters:OpenAi-ApiKey=""
Parameters:RobotLogin-PageUrl=""
Parameters:RobotLogin-Password=""
Parameters:RobotLogin-Username=""
Parameters:RobotVerifier-PageUrl=""
```

If you are using Rider, you can create a `.run/Agent.AppHost.run.xml` file and use it to run the project:

```xml
<component name="ProjectRunConfigurationManager">
    <configuration default="false" name="Agent.AppHost" type="AspireHostConfiguration" factoryName="Aspire Host" activateToolWindowBeforeRun="false">
        <option name="PROJECT_FILE_PATH" value="$PROJECT_DIR$/src/Agent.AppHost/Agent.AppHost.csproj" />
        <option name="PROJECT_TFM" value="net9.0" />
        <option name="LAUNCH_PROFILE_NAME" value="https" />
        <option name="TRACK_ARGUMENTS" value="1" />
        <option name="ARGUMENTS" value="" />
        <option name="TRACK_WORKING_DIRECTORY" value="1" />
        <option name="WORKING_DIRECTORY" value="$PROJECT_DIR$/src/Agent.AppHost" />
        <option name="TRACK_ENVS" value="0" />
        <envs>
            <env name="ASPNETCORE_ENVIRONMENT" value="Development" />
            <env name="ASPNETCORE_URLS" value="https://localhost:17095;http://localhost:15134" />
            <env name="DOTNET_DASHBOARD_OTLP_ENDPOINT_URL" value="https://localhost:21160" />
            <env name="DOTNET_ENVIRONMENT" value="Development" />
            <env name="DOTNET_LAUNCH_PROFILE" value="https" />
            <env name="DOTNET_RESOURCE_SERVICE_ENDPOINT_URL" value="https://localhost:22257" />
            <env name="Parameters:JsonCompleter-ApiKey" value="" />
            <env name="Parameters:JsonCompleter-ReportUrl" value="" />
            <env name="Parameters:OpenAi-ApiKey" value="" />
            <env name="Parameters:RobotLogin-PageUrl" value="" />
            <env name="Parameters:RobotLogin-Password" value="" />
            <env name="Parameters:RobotLogin-Username" value="" />
            <env name="Parameters:RobotVerifier-PageUrl" value="" />
        </envs>
        <option name="USE_PODMAN_RUNTIME" value="0" />
        <option name="TRACK_URL" value="1" />
        <browser url="https://localhost:17095" start="true" />
        <method v="2">
            <option name="Build" />
        </method>
    </configuration>
</component>
```
