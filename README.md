# Catchy - A caching proxy for local development

Developers often need to integrate with third-party webservices, even when developing locally.
These third-party webservices can lead to slower development speed, especially because test environments
tend to be slower than production environments. **Catchy** helps you speed back up!

1. Start Catchy from the commandline, providing the hostnames of your third-party webservices and how to cache them.
1. Cachy will automatically configure itself as your system proxy.
1. The first time Catchy sees a request that matches a provided hostname, it passes through the request and caches the response.
1. For all subsequent requests, Catchy returns the cached response.

Catchy comes with a couple of "caching strategies" out of the box:

- Cache By REST request -- for each unique URL / HTTP Method / request body, cache the corresponding response.
- Cache By SOAP body -- caches by the SOAP request body (excluding SOAP headers), because SOAP requests can have freqently changing SOAP headers.

You can add your own strategy by implementing an `ICacheStrategy`.

This application has been tested on Windows 10. It should in theory work on Mac OS and Linux, but has
not been tested.

## Usage

For example, if you're developing an application that integrates with two REST APIs, 
`worldtimeapi.org` and `hn.algolia.com`, you could configure Catchy like this:

```console
C:\> Catchy --CacheByRestRequest worldtimeapi.org --CacheByRestRequest hn.algolia.com
```

![demo](https://raw.githubusercontent.com/waf/catchy/master/demo.gif)

## Installation

For now, you need to download the source and build it using the dotnet command line or Visual Studio 2017.
If there's interest, this could be bundled as a dotnet global tool in the future.

## Contributing

Catchy is a C# .NET Core application, using the [Titanium Web Proxy](https://github.com/justcoding121/Titanium-Web-Proxy).
I'd love contributors to help out with this! If you're contributing code, please try to include unit tests with your code changes.
