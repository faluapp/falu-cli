# Falu CLI

[![Release](https://img.shields.io/github/release/faluapp/falu-cli.svg?style=flat-square)](https://github.com/faluapp/falu-cli/releases/latest)
[![GitHub Workflow Status](https://github.com/faluapp/falu-cli/actions/workflows/build.yml/badge.svg)](https://github.com/faluapp/falu-cli/actions/workflows/build.yml)

The official CLI tool for [Falu][falu] to help you build, test, and manage your Falu integration right from the terminal.

Available features:

- Manage your message templates in bulk (or via CI)
- Tail your API request logs for real-time insights.
- Resend webhook events effortlessly for simplified testing.
- Test webhooks securely, eliminating the need for external software.

## Usage

Installing the CLI provides access to the `falu` command.

```bash
falu [command]
```

```bash
# Run `-h` for detailed information about the tool
falu -h

# Run `-h` for detailed information about commands
falu [command] -h
```

### Commands

The Falu CLI supports a broad range of commands including:

- [`login`][wiki-command-login]
- [`logout`][wiki-command-logout]
- [`events retry`][wiki-command-events-retry]
- [`templates pull`][wiki-command-templates-pull]
- [`templates push`][wiki-command-templates-push]

Check out the [wiki](/wiki) for more on using the CLI.

## Installation

Falu CLI is available for macOS, Windows and Linux (Ubuntu). You can download each of the binaries in the [releases][releases] or you can use package managers in the respective platforms.

### macOS

Falu CLI is available on macOS via [Homebrew](https://brew.sh/):

```sh
brew install faluapp/falu-cli/falu
```

### Windows

Falu CLI is available on Windows via [Scoop](https://scoop.sh/) package manager:

```bash
scoop bucket add falu https://github.com/faluapp/scoop-falu-cli.git
scoop install falu
```

### Docker

The CLI is also available as a Docker image: [`ghcr.io/faluapp/falu-cli`](https://github.com/faluapp/falu-cli/pkgs/container/falu-cli).

```bash
docker run --rm -it ghcr.io/faluapp/falu-cli --version
a.b.c+commit
```

## Issues & Comments

Feel free to contact us if you encounter any issues with the library.
Please leave all comments, bugs, requests and issues on the Issues page.

## Development

For any requests, bug or comments, please [open an issue][issues] or [submit a pull request][pulls].

[chocolatey]: https://chocolatey.org/
[issues]: https://github.com/faluapp/falu-cli/issues/new
[pulls]: https://github.com/faluapp/falu-cli/pulls
[releases]: https://github.com/faluapp/falu-cli/releases
[falu]: https://falu.io
[wiki-command-login]: https://github.com/faluapp/falu-cli/wiki/commands/login
[wiki-command-logout]: https://github.com/faluapp/falu-cli/wiki/commands/logout
[wiki-command-events-retry]: https://github.com/faluapp/falu-cli/wiki/commands/events-retry
[wiki-command-templates-pull]: https://github.com/faluapp/falu-cli/wiki/commands/templates-pull
[wiki-command-templates-push]: https://github.com/faluapp/falu-cli/wiki/commands/templates-push

### License

The Library is licensed under the [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form") license. Refer to the [LICENSE](./LICENSE) file for more information.
