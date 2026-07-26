//
//  SlideTransition.swift
//  PerceptronDemo
//
//  A horizontal "pan across the desk" modal transition: presenting slides the
//  current screen out to the left while the new one slides in from the right;
//  dismissing runs it in reverse. Used to move between the main panel and the
//  Signal Flow screen, which read as two halves of one bench.
//
//  Keep a strong reference to the delegate — `UIViewController.transitioning-
//  Delegate` is weak.
//

import UIKit

final class SlideTransitionDelegate: NSObject, UIViewControllerTransitioningDelegate {

    func animationController(forPresented presented: UIViewController,
                             presenting: UIViewController,
                             source: UIViewController) -> (any UIViewControllerAnimatedTransitioning)? {
        SlideAnimator(isPresenting: true)
    }

    func animationController(forDismissed dismissed: UIViewController)
    -> (any UIViewControllerAnimatedTransitioning)? {
        SlideAnimator(isPresenting: false)
    }
}

final class SlideAnimator: NSObject, UIViewControllerAnimatedTransitioning {

    private let isPresenting: Bool

    init(isPresenting: Bool) {
        self.isPresenting = isPresenting
    }

    func transitionDuration(using transitionContext: (any UIViewControllerContextTransitioning)?) -> TimeInterval {
        0.45
    }

    func animateTransition(using transitionContext: any UIViewControllerContextTransitioning) {
        let container = transitionContext.containerView
        guard let fromView = transitionContext.view(forKey: .from),
              let toView = transitionContext.view(forKey: .to) else {
            transitionContext.completeTransition(false)
            return
        }

        // Presenting: incoming screen waits off the right edge and the outgoing
        // one leaves to the left. Dismissing: mirrored.
        let width = container.bounds.width
        let incomingStart = isPresenting ? width : -width
        let outgoingEnd = isPresenting ? -width : width

        toView.frame = container.bounds
        toView.transform = CGAffineTransform(translationX: incomingStart, y: 0)
        container.addSubview(toView)

        UIView.animate(withDuration: transitionDuration(using: transitionContext),
                       delay: 0,
                       options: [.curveEaseInOut]) {
            toView.transform = .identity
            fromView.transform = CGAffineTransform(translationX: outgoingEnd, y: 0)
        } completion: { _ in
            // The outgoing view may be reused (a full-screen presentation keeps
            // the presenter around), so hand it back untransformed.
            fromView.transform = .identity
            toView.transform = .identity
            transitionContext.completeTransition(!transitionContext.transitionWasCancelled)
        }
    }
}
