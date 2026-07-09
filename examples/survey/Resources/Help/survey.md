# survey

Collect a profile and return it as structured data.

[Back to main help](help:help)

## Usage

```
survey                                    Launch the interactive Terminal.Gui wizard
survey --name Ada --age 36 --sport Fencing
survey --name Ada --fruits "Apple,Cherry" --json
```

## Options

| Option | Type | Description |
|--------|------|-------------|
| `--name`, `-n` | string | The person's name. |
| `--fruits`, `-f` | string | Comma-separated list of favorite fruits. |
| `--sport`, `-s` | string | Favorite sport. |
| `--age`, `-a` | integer | Age in years (1-120). |
| `--password`, `-p` | string | Password (secret). |
| `--color`, `-c` | string | Favorite color (optional). |
| `--confirm` | flag | Show a confirmation step before finishing. |

## Behavior

When `--name` is provided, the command runs headless and returns the profile. With
`--json` it emits the full `SurveyAnswers` object. With no name in an interactive
terminal, it launches a Terminal.Gui Wizard (press Enter to accept, Esc to quit).
