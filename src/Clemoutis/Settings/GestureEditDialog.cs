using Clemoutis.Core.Actions;
using Clemoutis.Core.Gestures;

namespace Clemoutis.Settings;

/// <summary>
/// 1つのジェスチャー（ストローク列＋アクション）を編集する小ダイアログ。
/// ストロークは U/D/L/R 文字列、アクションは キー送信 / コマンド / 閉じる。
/// </summary>
internal sealed class GestureEditDialog : Form
{
    private readonly TextBox _strokes = new() { CharacterCasing = CharacterCasing.Upper };
    private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _keys = new();
    private readonly ComboBox _command = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _paramLabel = new();

    public GestureBinding? Result { get; private set; }

    public GestureEditDialog(GestureBinding? existing)
    {
        Text = existing is null ? "ジェスチャーの追加" : "ジェスチャーの編集";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(360, 200);

        var strokeLabel = new Label { Text = "ストローク (U/D/L/R)", Left = 12, Top = 15, Width = 140 };
        _strokes.SetBounds(160, 12, 180, 23);

        var typeLabel = new Label { Text = "アクション種別", Left = 12, Top = 48, Width = 140 };
        _type.SetBounds(160, 45, 180, 23);
        _type.Items.AddRange(ActionDisplay.TypeNames);
        _type.SelectedIndexChanged += (_, _) => UpdateParamVisibility();

        _paramLabel.SetBounds(12, 81, 140, 23);
        _keys.SetBounds(160, 78, 180, 23);
        _command.SetBounds(160, 78, 180, 23);
        _command.Items.AddRange(Enum.GetNames<AppCommand>());

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 174, Top = 150, Width = 80 };
        var cancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Left = 260, Top = 150, Width = 80 };
        ok.Click += OnOk;

        Controls.AddRange(new Control[]
        {
            strokeLabel, _strokes, typeLabel, _type, _paramLabel, _keys, _command, ok, cancel,
        });
        AcceptButton = ok;
        CancelButton = cancel;

        LoadExisting(existing);
        UpdateParamVisibility();
    }

    private void LoadExisting(GestureBinding? existing)
    {
        if (existing is null)
        {
            _type.SelectedItem = ActionDisplay.TypeKey;
            return;
        }
        _strokes.Text = existing.Strokes;
        _type.SelectedItem = ActionDisplay.TypeNameOf(existing.Action);
        switch (existing.Action)
        {
            case KeyAction k:
                _keys.Text = k.Stroke.ToString();
                break;
            case AppCommandAction c:
                _command.SelectedItem = c.Command.ToString();
                break;
        }
    }

    private void UpdateParamVisibility()
    {
        string type = (string?)_type.SelectedItem ?? ActionDisplay.TypeKey;
        bool isKey = type == ActionDisplay.TypeKey;
        bool isCmd = type == ActionDisplay.TypeAppCommand;
        _keys.Visible = isKey;
        _command.Visible = isCmd;
        _paramLabel.Visible = isKey || isCmd;
        _paramLabel.Text = isKey ? "キー (例 Ctrl+W)" : isCmd ? "コマンド" : "";
        if (isCmd && _command.SelectedIndex < 0 && _command.Items.Count > 0)
            _command.SelectedIndex = 0;
    }

    private void OnOk(object? sender, EventArgs e)
    {
        string strokes = _strokes.Text.Trim();
        if (!IsValidStrokes(strokes))
        {
            Warn("ストロークは U/D/L/R の組み合わせで入力してください（例: DR）。");
            return;
        }

        string type = (string?)_type.SelectedItem ?? ActionDisplay.TypeKey;
        GestureAction action;
        if (type == ActionDisplay.TypeKey)
        {
            if (!KeyStrokeParser.TryParse(_keys.Text, out var stroke, out var error))
            {
                Warn(error);
                return;
            }
            action = new KeyAction(stroke);
        }
        else if (type == ActionDisplay.TypeAppCommand)
        {
            if (_command.SelectedItem is not string name
                || !Enum.TryParse<AppCommand>(name, out var cmd))
            {
                Warn("コマンドを選択してください。");
                return;
            }
            action = new AppCommandAction(cmd);
        }
        else
        {
            action = new CloseAction();
        }

        Result = new GestureBinding(strokes, action);
        DialogResult = DialogResult.OK;
    }

    private static bool IsValidStrokes(string s) =>
        s.Length > 0 && s.All(c => c is 'U' or 'D' or 'L' or 'R');

    private void Warn(string message)
    {
        MessageBox.Show(this, message, "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        DialogResult = DialogResult.None;
    }
}
