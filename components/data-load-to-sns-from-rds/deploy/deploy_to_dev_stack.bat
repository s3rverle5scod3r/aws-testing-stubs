@echo off
rem This script will automatically deploy the stack in a dev environment: use this for a feature branch.
rem Parameters:
rem %1: Version (String) : The major, minor and subminor version of this deployment.
rem Execute usage: 'deploy_to_dev_stack.bat %1'
rem Refer to GitLab/GitHub environment variables on CI configuration for current version.

rem This assumes that within the root of your project folder you have a folder named deploy where you store your deployment scripts.

rem Change directory to root.
cd..

echo Building...
dotnet restore
dotnet clean
dotnet build -c Release
dotnet publish -c Release
echo Build completed.

echo Deploying..
cmd /C call scripts/sam_command_dev.bat %1
echo Deployment completed.

rem Change directory back to deploy for future deployments.
cd deploy

echo Script completed. Last updated %time%
pause