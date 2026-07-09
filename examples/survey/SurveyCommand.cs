using System.Collections.ObjectModel;
using System.Globalization;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Interop.Spectre;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;
using Color = Spectre.Console.Color;

namespace Terminal.Gui.Cli.Survey;

/// <summary>
///     An input command that collects a person's profile via a Wizard and returns it as a typed
///     <see cref="SurveyAnswers" />. With <c>--name</c> (or other options) it runs headless and emits
///     a structured <c>--json</c> envelope; otherwise it launches an interactive Terminal.Gui Wizard.
/// </summary>
public sealed class SurveyCommand : ICliCommand<SurveyAnswers>
{
    /// <inheritdoc />
    public string PrimaryAlias => "survey";

    /// <inheritdoc />
    public IReadOnlyList<string> Aliases { get; } = ["survey"];

    /// <inheritdoc />
    public string Description => "Collect a profile and return it as structured data.";

    /// <inheritdoc />
    public CommandKind Kind => CommandKind.Input;

    /// <inheritdoc />
    public Type ResultType => typeof (SurveyAnswers);

    /// <inheritdoc />
    public IReadOnlyList<CommandOptionDescriptor> Options => ProfileInput.Options;

    /// <inheritdoc />
    public bool AcceptsPositionalArgs => true;

    /// <inheritdoc />
    public async Task<CommandResult<SurveyAnswers>> RunAsync (
        IApplication app,
        string? initial,
        CommandRunOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull (app);

        if (!ProfileInput.TryBuild (options, initial, out SurveyAnswers answers, out var error))
        {
            return new CommandResult<SurveyAnswers> (CommandStatus.Error, null, "validation", error);
        }

        if (!string.IsNullOrWhiteSpace (answers.Name))
        {
            return new CommandResult<SurveyAnswers> (CommandStatus.Ok, answers, null, null);
        }

        if (Console.IsInputRedirected)
        {
            return new CommandResult<SurveyAnswers> (
                CommandStatus.Error,
                null,
                "validation",
                "A name is required. Pass --name in non-interactive mode.");
        }

        var confirm = options.CommandOptions.ContainsKey ("confirm");
        SurveyAnswers? captured = await RunWizardAsync (app, confirm, cancellationToken);

        if (captured is null)
        {
            return new CommandResult<SurveyAnswers> (CommandStatus.Cancelled, null, null, null);
        }

        return new CommandResult<SurveyAnswers> (CommandStatus.Ok, captured, null, null);
    }

    private static async Task<SurveyAnswers?> RunWizardAsync (
        IApplication app,
        bool confirm,
        CancellationToken cancellationToken)
    {
        Wizard wizard = new ()
        {
            Title = "Survey - Enter to accept, Esc to quit",
            Width = Dim.Fill (),
            Height = 17, // tall enough for the largest step (fruits tree fully expanded + label + buttons)
            BorderStyle = LineStyle.Rounded,
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent),
            ShadowStyle = null
        };
        wizard.Border.Thickness = new Thickness (0, 1, 0, 0);

        // --- Step 1: Name ---
        WizardStep nameStep = new ()
        {
            Title = "Name",
            HelpText = """
                       ## Your Name

                       Enter your **full name** or a nickname.

                       This will be displayed on your profile card.

                       > *Tip:* Press `Tab` to move to the Next button.
                       """
        };
        Label nameLabel = new () { Text = "_Name:" };
        TextField nameField = new ()
        {
            X = Pos.Right (nameLabel) + 1,
            Y = 0,
            Width = Dim.Percent (50)
        };
        nameStep.Add (nameLabel, nameField);
        wizard.AddStep (nameStep);

        // --- Step 2: Favorite Fruits (multi-select) ---
        WizardStep fruitsStep = new ()
        {
            Title = "Fruits",
            HelpText = """
                       ## Favorite Fruits

                       Select your favorites from the list:

                       - Press `Space` to toggle a selection
                       - Use `↑`/`↓` to navigate

                       ### Categories

                       Some items are grouped under **Berries**:

                       1. Strawberry
                       2. Blueberry
                       3. Raspberry
                       """
        };
        Label fruitsLabel = new () { Text = "_Favorite fruits (Space to toggle):" };
        TreeView fruitsTree = CreateFruitsTreeView ();

        fruitsStep.Add (fruitsLabel, fruitsTree);
        wizard.AddStep (fruitsStep);

        // --- Step 3: Conditional single-pick (only if >1 fruit selected) ---
        WizardStep favFruitStep = new ()
        {
            Title = "Favorite Fruit",
            HelpText = """
                       ## Pick One

                       You selected *multiple* fruits — now choose your **absolute favorite**.

                       This step only appears when more than one fruit is selected.
                       """
        };
        Label favFruitLabel = new () { Text = "Ok, but if you could only choose _one:" };
        ListView favFruitList = new ()
        {
            X = 0,
            Y = 1,
            Width = Dim.Auto (),
            Height = Dim.Fill ()
        };
        favFruitStep.Add (favFruitLabel, favFruitList);
        wizard.AddStep (favFruitStep);

        // --- Step 4: Favorite Sport ---
        WizardStep sportStep = new ()
        {
            Title = "Sport",
            HelpText = """
                       ## Favorite Sport

                       Pick from the list or type your own.

                       - **Soccer** – The beautiful game
                       - **Hockey** – Fast-paced ice sport
                       - **Basketball** – Slam dunks!

                       > If you type a custom sport, the selector deselects.
                       """
        };
        Label sportLabel = new () { Text = "Favorite _sport:" };
        OptionSelector sportSelector = new ()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Labels = ["Soccer", "Hockey", "Basketball"],
            Value = null
        };
        Label sportOrLabel = new ()
        {
            X = 0,
            Y = Pos.Bottom (sportSelector) + 1,
            Text = "Or _type your own:"
        };
        TextField sportTextField = new ()
        {
            X = Pos.Right (sportOrLabel) + 1,
            Y = Pos.Bottom (sportSelector) + 1,
            Width = Dim.Percent (50)
        };

        sportSelector.ValueChanged += (_, args) =>
        {
            if (args.NewValue is >= 0 && args.NewValue < sportSelector.Labels!.Count)
            {
                sportTextField.Text = sportSelector.Labels[args.NewValue.Value];
            }
        };

        sportTextField.TextChanged += (_, _) =>
        {
            var text = sportTextField.Text.Trim ();

            if (sportSelector.Value is not null &&
                !string.Equals (text, sportSelector.Labels![sportSelector.Value.Value],
                    StringComparison.OrdinalIgnoreCase))
            {
                sportSelector.Value = null;
            }
        };

        sportStep.Add (sportLabel, sportSelector, sportOrLabel, sportTextField);
        wizard.AddStep (sportStep);

        // --- Step 5: Age (validated) ---
        WizardStep ageStep = new ()
        {
            Title = "Age",
            HelpText = """
                       ## Your Age

                       Enter a number between **1** and **120**.

                       ### Validation Rules

                       - Must be a whole number
                       - No letters or symbols
                       - Range: `1–120`

                       An error message will appear if the value is invalid.
                       """
        };
        Label ageLabel = new () { Text = "_Age (1-120):" };
        TextField ageField = new ()
        {
            X = Pos.Right (ageLabel) + 1,
            Y = 0,
            Width = Dim.Percent (50)
        };
        Label ageError = new ()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Visible = false
        };
        ageStep.Add (ageLabel, ageField, ageError);
        wizard.AddStep (ageStep);

        // --- Step 6: Password ---
        WizardStep passwordStep = new ()
        {
            Title = "Password",
            HelpText = """
                       ## Password

                       Enter a secret password. Characters are masked with `*`.

                       ### Guidelines

                       - **Minimum length:** none (for this demo)
                       - Input is *not* echoed to the terminal
                       - Stored only for display in the results card
                       """
        };
        Label passwordLabel = new () { Text = "_Password:" };
        TextField passwordField = new ()
        {
            X = Pos.Right (passwordLabel) + 1,
            Y = 0,
            Width = Dim.Percent (50),
            Secret = true
        };
        passwordStep.Add (passwordLabel, passwordField);
        wizard.AddStep (passwordStep);

        // --- Step 7: Favorite Color ---
        WizardStep colorStep = new ()
        {
            Title = "Color",
            HelpText = """
                       ## Favorite Color

                       Use the **ColorPicker** to choose a color:

                       - Adjust `H`, `S`, `V` sliders
                       - Or type a hex value directly (e.g. `#FF6600`)
                       - The color name is shown below

                       ### Color Spaces

                       The picker supports [HSV](https://en.wikipedia.org/wiki/HSL_and_HSV) color space.
                       """
        };
        Label colorLabel = new () { Text = "Favorite _color:" };
        ColorPicker colorPicker = new ()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        colorPicker.Style.ShowTextFields = true;
        colorPicker.Style.ShowColorName = true;
        colorPicker.ApplyStyleChanges ();
        colorStep.Add (colorLabel, colorPicker);
        wizard.AddStep (colorStep);

        // --- Optional Step 8: Confirmation with Spectre card rendering ---
        WizardStep? confirmStep = null;
        SpectreView? confirmView = null;

        if (confirm)
        {
            confirmStep = new WizardStep
            {
                Title = "Confirm",
                HelpText = """
                           ## Review & Confirm

                           Check your answers in the table on the left.

                           - Press **Finish** to accept
                           - Press **Back** to make changes
                           - Press `Esc` to cancel entirely

                           > Your results will be printed to the terminal after confirmation.
                           """
            };
            Label confirmLabel = new () { Text = "Review your answers and press Finish to _confirm:" };
            confirmView = new SpectreView
            {
                X = 0,
                Y = 2,
                Width = Dim.Fill (),
                Height = Dim.Fill ()
            };
            confirmStep.Add (confirmLabel, confirmView);
            wizard.AddStep (confirmStep);
        }

        // --- Step navigation ---
        // Track direction so we only auto-skip the favFruitStep when moving forward.
        var movingForward = true;
        wizard.MovingBack += (_, _) => movingForward = false;
        wizard.MovingNext += (_, _) => movingForward = true;

        wizard.StepChanged += (_, _) =>
        {
            if (wizard.CurrentStep == favFruitStep)
            {
                List<string> selected = GetSelectedFruits (fruitsTree);

                switch (movingForward)
                {
                    case true when selected.Count <= 1:
                        wizard.GoNext ();
                        break;
                    case false when selected.Count <= 1:
                        wizard.GoBack ();
                        break;
                    default:
                        favFruitList.SetSource (new ObservableCollection<string> (selected));
                        break;
                }
            }
            else if (wizard.CurrentStep == confirmStep && confirmView is not null)
            {
                SurveyAnswers preview = BuildAnswers (
                    nameField, fruitsTree, favFruitList, sportTextField, ageField, passwordField, colorPicker);

                // Get the background color from the wizard step so the table blends in
                Attribute attr = confirmStep!.GetAttributeForRole (VisualRole.Normal);
                Color? spectreBg = null;

                if (attr is { Background: var tgBg } && tgBg != Drawing.Color.None)
                {
                    spectreBg = new Color (tgBg.R, tgBg.G, tgBg.B);
                }

                confirmView.Renderable = SpectreProfile.Build (preview, spectreBg);
            }
        };

        // Validate age before allowing advancement past the age step
        wizard.MovingNext += (_, args) =>
        {
            if (wizard.CurrentStep != ageStep)
            {
                return;
            }

            var ageText = ageField.Text.Trim ();

            if (ageText.Length == 0 ||
                !int.TryParse (ageText, NumberStyles.None, CultureInfo.InvariantCulture, out var age) ||
                age < 1 || age > 120)
            {
                ageError.Text = "Please enter a valid age between 1 and 120.";
                ageError.Visible = true;
                args.Cancel = true;
            }
            else
            {
                ageError.Visible = false;
            }
        };

        // Capture results when wizard finishes
        SurveyAnswers? result = null;

        wizard.Accepted += (_, _) =>
        {
            result = BuildAnswers (
                nameField, fruitsTree, favFruitList, sportTextField, ageField, passwordField, colorPicker);
        };

        await app.RunAsync (wizard, cancellationToken);

        return result;
    }

    private static SurveyAnswers BuildAnswers (
        TextField nameField,
        TreeView fruitsTree,
        ListView favFruitList,
        TextField sportTextField,
        TextField ageField,
        TextField passwordField,
        ColorPicker colorPicker)
    {
        var name = nameField.Text.Trim ();
        List<string> selectedFruits = GetSelectedFruits (fruitsTree);

        var favoriteFruit = selectedFruits.Count == 1
            ? selectedFruits[0]
            : favFruitList.Value is >= 0 && favFruitList.Value < (favFruitList.Source?.Count ?? 0)
                ? favFruitList.Source!.ToList ()[favFruitList.Value.Value]?.ToString ()
                : selectedFruits.Count > 0
                    ? selectedFruits[0]
                    : null;

        var sport = sportTextField.Text.Trim ();

        if (sport.Length == 0)
        {
            sport = "Unspecified";
        }

        var ageText = ageField.Text.Trim ();
        int.TryParse (ageText, NumberStyles.None, CultureInfo.InvariantCulture, out var age);

        var password = passwordField.Text ?? string.Empty;
        var color = colorPicker.SelectedColor.ToString ();

        return new SurveyAnswers (name, selectedFruits, favoriteFruit, sport, age, password, color);
    }

    /// <summary>Builds the hierarchical fruit tree with "Berries" as a parent category.</summary>
    private static TreeView CreateFruitsTreeView ()
    {
        TreeView tree = new ()
        {
            X = 0,
            Y = 1,
            Width = Dim.Auto (),
            Height = Dim.Fill (),
            CheckboxMode = true
        };

        tree.AddObject (new TreeNode { Text = "Apple" });
        tree.AddObject (new TreeNode { Text = "Apricot" });
        tree.AddObject (new TreeNode { Text = "Banana" });

        tree.AddObject (new TreeNode
        {
            Text = "Berries",
            Children =
            [
                new TreeNode { Text = "Blackberry" },
                new TreeNode { Text = "Blueberry" },
                new TreeNode { Text = "Raspberry" },
                new TreeNode { Text = "Strawberry" }
            ]
        });

        tree.AddObject (new TreeNode { Text = "Mango" });
        tree.AddObject (new TreeNode { Text = "Orange" });
        tree.AddObject (new TreeNode { Text = "Pear" });

        tree.ExpandAll ();

        return tree;
    }

    /// <summary>Gets the list of checked leaf-node fruit names from the tree.</summary>
    private static List<string> GetSelectedFruits (TreeView fruitsTree)
    {
        return fruitsTree.GetCheckedObjects ()
            .Where (node => node.Children.Count == 0) // leaf nodes only (skip "Berries" category)
            .Select (node => node.Text)
            .ToList ();
    }
}
