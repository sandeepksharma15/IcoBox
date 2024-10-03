
# IcoBox

### Build and Run

1. Open the solution in Visual Studio.
2. Build the solution.
3. Run the application.

## Code Overview

### IconBox.cs

The `IconBox` class inherits from `Form` and customizes the window properties. It includes:

- P/Invoke methods to interact with Windows API for window styles.
- Custom title bar with a `Label` for the title.
- Event handlers for dragging the window and editing the title.

### Key Methods

- `TitleLabel_DoubleClick(object sender, EventArgs e)`: Allows editing of the title label on double-click.
- `ConfirmTitleChange(TextBox textBox)`: Updates the title label with the new text.
- `HeaderPanel_MouseMove(object sender, MouseEventArgs e)`: Handles dragging of the window.
- `HeaderPanel_MouseDown(object sender, MouseEventArgs e)`: Starts the dragging process.
- `HeaderPanel_MouseUp(object sender, MouseEventArgs e)`: Stops the dragging process.
- `GetWindowBounds()`: Calculates the initial size and position of the window.

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contact

For any questions or suggestions, please open an issue or contact [sandeepksharma15](mailto:sandeepksharma15@gmail.com).
