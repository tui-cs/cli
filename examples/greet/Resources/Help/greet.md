# greet

Prompt for a name and return a greeting.

[Back to main help](help:help)

## Usage

```
greet [name]
greet --formal [name]
```

## Options

| Option | Type | Description | Default |
|--------|------|-------------|---------|
| `--formal`, `-f` | bool | Use a formal greeting style | `false` |

## Examples

```
$ greet World
Hello, World!

$ greet --formal Alice
Good day, Alice. It is a pleasure to meet you.

$ greet Charlie
Hello, Charlie!
```

## Notes

If no name is provided, the greeting defaults to "World".
The `--formal` flag switches from a casual "Hello" to a more
formal greeting style.
