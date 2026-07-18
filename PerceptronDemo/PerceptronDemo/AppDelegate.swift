//
//  AppDelegate.swift
//  PerceptronDemo
//
//  Created by Adam Wulf on 7/17/26.
//

import UIKit

@main
class AppDelegate: UIResponder, UIApplicationDelegate {



    func application(_ application: UIApplication, didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?) -> Bool {
        // Override point for customization after application launch.
        return true
    }

#if DEBUG
    // MARK: - Debug Menu (Mac Catalyst)

    /// Size we resize windows to for capturing App Store screenshots.
    private static let appStoreScreenshotSize = CGSize(width: 1280, height: 800)

    private static let resizeForAppStoreCommand = "resizeForAppStore:"

    override func buildMenu(with builder: any UIMenuBuilder) {
        super.buildMenu(with: builder)

        // Only the menu bar (Mac Catalyst) gets this command.
        guard builder.system == .main else { return }

        let resizeItem = UIKeyCommand(
            title: "Resize for App Store (1280 × 800)",
            action: Selector(AppDelegate.resizeForAppStoreCommand),
            input: "0",
            modifierFlags: [.command, .control]
        )
        let menu = UIMenu(
            title: "",
            options: .displayInline,
            children: [resizeItem]
        )
        builder.insertChild(menu, atEndOfMenu: .help)
    }

    /// Resizes every connected foreground window scene to the App Store
    /// screenshot size. Handled here because `AppDelegate` sits at the end
    /// of the responder chain, so the menu command reaches it regardless of
    /// which window is key.
    @objc func resizeForAppStore(_ sender: Any?) {
        let size = AppDelegate.appStoreScreenshotSize
        for scene in UIApplication.shared.connectedScenes {
            guard let windowScene = scene as? UIWindowScene,
                  windowScene.activationState == .foregroundActive
                    || windowScene.activationState == .foregroundInactive else {
                continue
            }
            let frame = CGRect(origin: windowScene.effectiveGeometry.systemFrame.origin, size: size)
            let preferences = UIWindowScene.GeometryPreferences.Mac(systemFrame: frame)
            windowScene.requestGeometryUpdate(preferences) { error in
                print("Resize for App Store failed for scene \(windowScene.session.persistentIdentifier): \(error)")
            }
        }
    }
#endif

    // MARK: UISceneSession Lifecycle

    func application(_ application: UIApplication, configurationForConnecting connectingSceneSession: UISceneSession, options: UIScene.ConnectionOptions) -> UISceneConfiguration {
        // Called when a new scene session is being created.
        // Use this method to select a configuration to create the new scene with.
        return UISceneConfiguration(name: "Default Configuration", sessionRole: connectingSceneSession.role)
    }

    func application(_ application: UIApplication, didDiscardSceneSessions sceneSessions: Set<UISceneSession>) {
        // Called when the user discards a scene session.
        // If any sessions were discarded while the application was not running, this will be called shortly after application:didFinishLaunchingWithOptions.
        // Use this method to release any resources that were specific to the discarded scenes, as they will not return.
    }


}

