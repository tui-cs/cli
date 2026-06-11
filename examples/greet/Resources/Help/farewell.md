# farewell

Say goodbye to someone.

[Back to main help](help:help)

## Usage

```
farewell [name]
farewell --until <time> [name]
```

## Options

| Option | Type | Description | Default |
|--------|------|-------------|---------|
| `--until`, `-u` | string | When you expect to meet again | - |

## Examples

```
$ farewell Bob
Goodbye, Bob!

$ farewell --until tomorrow Alice
Goodbye, Alice! See you tomorrow.

$ farewell --until "next week" World
Goodbye, World! See you next week.
```

## Notes

If no name is provided, the farewell defaults to "World".
Use `--until` to add a "see you" message with a time reference.
