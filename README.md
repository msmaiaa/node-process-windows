# node-window-switcher
###### Manage application windows via a Node API - set focus, cycle active windows, and get active windows

- [Installation](#Installation)
    - [Supported Platforms](#Supported_Platforms)
- [Usage](#Usage)
- [Contributing](#Contributing)
- [License](#License)
- [Contact](#Contact)

## Installation

Requires Node 12+

```
    npm install node-window-switcher
```

This module is __not supported__ in browsers.

### Supported Platforms

Currently, this module is only supported on Windows, and uses a .NET console app to manage windows.

Pull requests are welcome - it would be great to have this API work cross-platform.

## Usage

1) Get active processes

```javascript
    var processWindows = require("node-window-switcher");

    var activeProcesses = processWindows.getProcesses();
```

2) Focus a window

```javascript
    var processWindows = require("node-window-switcher");

    processWindows.focusWindow("chrome");
```

## Contributing

Pull requests are welcome

## License

[MIT License]("LICENSE")

## Contact

extr0py@extropygames.com
