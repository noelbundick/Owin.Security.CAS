# Owin.Security.CAS
Owin.Security.CAS is an [OWIN](http://owin.org) authentication provider for [CAS](https://github.com/Jasig/cas)

## Installing
Using NuGet

    PM> install-package Owin.Security.CAS

Using zip file

1. [Download the zip file](https://github.com/noelbundick/Owin.Security.CAS/archive/master.zip) by clicking on the **Download Zip File** button on the project home page on GitHub
2. Extract the zipped files. An **Owin.Security.CAS-master** folder will be created.
3. In Visual Studio, right click on your solution and select **Add > Existing Project**. The **Add Existing Project** window will appear
4. Navigate to **Owin.Security.CAS > Owin.Security.CAS.csproj** and click **Open**.  The project will now be in your solution.
5. Make your project dependenent on **Owin.Security.CAS** so that any updates you download and unzip in the **Owin.Security.CAS-master** folder will cause the dll to be recompiled
  1. Selecting **Project > Project Dependencies** from the Visual Studio menu. The **Project Dependencies** window will appear.
  2. Select the **Dependencies** tab
  3. Select your project from the **Projects** dropdown
  4. Check **Owin.Security.CAS** in the **Depends on** area
  5. Click **OK**
6. Add a reference to the **Owin.Security.CAS** project so that it can be used in your code
  1. In the **References** section of your solution, right-click and select **Add Reference...**.  The **Reference Manager** window willl appear.
  2. Select **Solution > Projects**
  3. Check the box for **Owin.Security.CAS**
  4. Select **OK**
  5. You should now see **Owin.Security.CAS** under **References**

## Properties
## Methods
## Examples

[![MIT license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/noelbundick/Owin.Security.CAS/blob/master/LICENSE.md)
[![Build Status](https://www.myget.org/BuildSource/Badge/owin-security-cas?identifier=f61417a1-8dfe-49f2-9981-b9d44c5b234e)](https://www.myget.org/)

