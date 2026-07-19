//
//  SceneDelegate.swift
//  PerceptronDemo
//
//  Created by Adam Wulf on 7/17/26.
//

import UIKit

class SceneDelegate: UIResponder, UIWindowSceneDelegate {

    var window: UIWindow?


    func scene(_ scene: UIScene, willConnectTo session: UISceneSession, options connectionOptions: UIScene.ConnectionOptions) {
        // Build the UI programmatically (no storyboard) so the app launches
        // straight into the perceptron panel on a black background.
        guard let windowScene = scene as? UIWindowScene else { return }
        let window = UIWindow(windowScene: windowScene)
        let panel = PerceptronPanelViewController()
        window.rootViewController = panel
        self.window = window
        window.makeKeyAndVisible()

        // Show the introduction on every launch (not just the first) so a
        // first-time user always gets context about the machine. Deferred to
        // the next runloop so the panel is in the window hierarchy first —
        // presenting straight from willConnectTo, before the panel has
        // appeared, silently drops the modal.
        DispatchQueue.main.async { [weak panel] in
            guard let panel else { return }
            self.presentIntro(over: panel)
        }
    }

    /// Presents the introduction card modally over the panel. On Mac Catalyst
    /// and iPad this comes up as a centered sheet; on iPhone it's full-screen.
    private func presentIntro(over panel: UIViewController) {
        let intro = IntroViewController()
        intro.modalPresentationStyle = .formSheet
        intro.modalTransitionStyle = .coverVertical
        intro.isModalInPresentation = true // require the BEGIN button to dismiss
        panel.present(intro, animated: true)
    }

    func sceneDidDisconnect(_ scene: UIScene) {
        // Called as the scene is being released by the system.
        // This occurs shortly after the scene enters the background, or when its session is discarded.
        // Release any resources associated with this scene that can be re-created the next time the scene connects.
        // The scene may re-connect later, as its session was not necessarily discarded (see `application:didDiscardSceneSessions` instead).
    }

    func sceneDidBecomeActive(_ scene: UIScene) {
        // Called when the scene has moved from an inactive state to an active state.
        // Use this method to restart any tasks that were paused (or not yet started) when the scene was inactive.
    }

    func sceneWillResignActive(_ scene: UIScene) {
        // Called when the scene will move from an active state to an inactive state.
        // This may occur due to temporary interruptions (ex. an incoming phone call).
    }

    func sceneWillEnterForeground(_ scene: UIScene) {
        // Called as the scene transitions from the background to the foreground.
        // Use this method to undo the changes made on entering the background.
    }

    func sceneDidEnterBackground(_ scene: UIScene) {
        // Called as the scene transitions from the foreground to the background.
        // Use this method to save data, release shared resources, and store enough scene-specific state information
        // to restore the scene back to its current state.
    }


}

