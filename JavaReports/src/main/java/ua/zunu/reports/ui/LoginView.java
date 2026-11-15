package ua.zunu.reports.ui;

import javafx.geometry.Insets;
import javafx.geometry.Pos;
import javafx.scene.Scene;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.control.PasswordField;
import javafx.scene.control.TextField;
import javafx.scene.layout.BorderPane;
import javafx.scene.layout.StackPane;
import javafx.scene.layout.VBox;
import javafx.stage.Stage;
import ua.zunu.reports.domain.Director;
import ua.zunu.reports.service.DirectorAuthenticationService;

import java.sql.SQLException;
import java.util.function.Consumer;

public final class LoginView {
    private final DirectorAuthenticationService authentication;

    public LoginView(DirectorAuthenticationService authentication) {
        this.authentication = authentication;
    }

    public void show(Stage stage, Consumer<Director> onAuthenticated) {
        var root = new BorderPane();
        root.getStyleClass().add("login-page");

        var brand = new Label("GARAGEFLOW");
        brand.getStyleClass().add("login-brand");
        var brandSubhead = new Label("SERVICE CENTER  /  MANAGEMENT");
        brandSubhead.getStyleClass().add("login-brand-subhead");
        var top = new VBox(5, brand, brandSubhead);
        top.setPadding(new Insets(38, 48, 20, 48));
        root.setTop(top);

        var art = new VBox(20);
        art.getStyleClass().add("login-art");
        art.setAlignment(Pos.CENTER_LEFT);
        art.setPadding(new Insets(40, 64, 50, 64));
        var artEyebrow = new Label("ЗВІТНІСТЬ СЕРВІСНОГО ЦЕНТРУ");
        artEyebrow.getStyleClass().add("eyebrow-light");
        var artTitle = new Label("Бачити більше.\nКерувати точніше.");
        artTitle.getStyleClass().add("login-art-title");
        var artText = new Label("Аналітика роботи, послуг і клієнтів в одному просторі.");
        artText.getStyleClass().add("login-art-text");
        art.getChildren().addAll(artEyebrow, artTitle, artText);
        root.setLeft(art);

        var login = new TextField();
        login.setPromptText("Логін керівника");
        login.setMaxWidth(Double.MAX_VALUE);
        login.getStyleClass().add("input");
        var password = new PasswordField();
        password.setPromptText("Пароль");
        password.setMaxWidth(Double.MAX_VALUE);
        password.getStyleClass().add("input");
        var error = new Label();
        error.getStyleClass().add("login-error");
        error.setWrapText(true);
        var button = new Button("Увійти до звітів  →");
        button.getStyleClass().addAll("button", "button-primary");
        button.setMaxWidth(Double.MAX_VALUE);

        var cardTitle = new Label("Вхід до системи");
        cardTitle.getStyleClass().add("login-card-title");
        var cardText = new Label("Доступ надається лише обліковому запису керівника.");
        cardText.getStyleClass().add("muted-text");
        var form = new VBox(13, cardTitle, cardText, fieldLabel("Логін", login), fieldLabel("Пароль", password), error, button);
        form.getStyleClass().add("login-form");
        form.setMaxWidth(390);
        form.setPadding(new Insets(38));
        StackPane card = new StackPane(form);
        card.setPadding(new Insets(32, 48, 48, 48));
        root.setCenter(card);

        var signIn = (Runnable) () -> {
            try {
                var director = authentication.authenticate(login.getText(), password.getText());
                if (director.isEmpty()) {
                    error.setText("Не вдалося увійти. Перевір логін і пароль керівника.");
                    return;
                }
                onAuthenticated.accept(director.get());
            } catch (IllegalArgumentException exception) {
                error.setText(exception.getMessage());
            } catch (SQLException exception) {
                error.setText("Не вдалося перевірити обліковий запис у базі даних.");
            }
        };
        button.setOnAction(event -> signIn.run());
        password.setOnAction(event -> signIn.run());

        var scene = new Scene(root, 1180, 760);
        scene.getStylesheets().add(getClass().getResource("/ua/zunu/reports/ui/theme.css").toExternalForm());
        stage.setTitle("GarageFlow · Звіти");
        stage.setMinWidth(980);
        stage.setMinHeight(680);
        stage.setScene(scene);
        stage.centerOnScreen();
        stage.show();
    }

    private static VBox fieldLabel(String title, TextField field) {
        var label = new Label(title);
        label.getStyleClass().add("field-label");
        return new VBox(7, label, field);
    }
}
