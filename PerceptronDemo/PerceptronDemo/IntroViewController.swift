//
//  IntroViewController.swift
//  PerceptronDemo
//
//  A first-context "briefing card" shown over the instrument panel on every
//  launch. It orients a first-time user: what the machine does, its 1958
//  history, an inventory of what's on the panel, and links out to Wikipedia
//  and the video that inspired the project.
//
//  Styled to match the panel's vintage aesthetic — black chassis, brushed
//  olive-grey plates, engraved monospaced text, a backlit BEGIN button.
//  Content lives in a scroll view so it fits any window size, iPhone through
//  Mac Catalyst.
//

import UIKit

final class IntroViewController: UIViewController {

    // MARK: - Palette (shared with the panel's controls)

    private enum Palette {
        static let chassis = UIColor.black
        static let plateTop = UIColor(red: 75/255, green: 80/255, blue: 75/255, alpha: 1)
        static let plateBottom = UIColor(red: 55/255, green: 60/255, blue: 55/255, alpha: 1)
        static let engraved = UIColor(red: 180/255, green: 185/255, blue: 175/255, alpha: 1)
        static let engravedDim = UIColor(red: 130/255, green: 135/255, blue: 130/255, alpha: 1)
        static let heading = UIColor(red: 150/255, green: 190/255, blue: 150/255, alpha: 1) // olive-green
        static let link = UIColor(red: 100/255, green: 180/255, blue: 100/255, alpha: 1)
        static let hairline = UIColor(red: 40/255, green: 45/255, blue: 40/255, alpha: 1)
        static let beginGlow = UIColor(red: 255/255, green: 220/255, blue: 80/255, alpha: 1)
    }

    private enum Font {
        static func mono(_ size: CGFloat, _ weight: UIFont.Weight = .regular) -> UIFont {
            UIFont.monospacedSystemFont(ofSize: size, weight: weight)
        }
    }

    // MARK: - Views

    private let scrollView = UIScrollView()
    private let contentStack = UIStackView()
    private let beginButton = PushButton()

    // MARK: - Lifecycle

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = Palette.chassis
        buildScaffold()
        buildContent()
    }

    // MARK: - Scaffold

    private func buildScaffold() {
        scrollView.translatesAutoresizingMaskIntoConstraints = false
        scrollView.alwaysBounceVertical = true
        scrollView.showsVerticalScrollIndicator = true
        scrollView.indicatorStyle = .white
        view.addSubview(scrollView)

        contentStack.translatesAutoresizingMaskIntoConstraints = false
        contentStack.axis = .vertical
        contentStack.alignment = .fill
        contentStack.spacing = 20
        scrollView.addSubview(contentStack)

        // BEGIN button pinned to the bottom, above the safe area, so it's
        // always reachable without scrolling to the end.
        beginButton.translatesAutoresizingMaskIntoConstraints = false
        beginButton.isSquare = true
        beginButton.labelText = "BEGIN"
        beginButton.glowColor = Palette.beginGlow
        beginButton.onTap = { [weak self] in self?.dismissIntro() }
        view.addSubview(beginButton)

        // Pin the stack to the scroll view's content guide on all four edges
        // (this drives the scrollable content height) and lock its width to the
        // *frame* guide so there is never any horizontal scroll or offset. The
        // reading measure is capped at 620pt by centering the stack and letting
        // it shrink below the padded full width on a wide Mac window.
        let sidePadding: CGFloat = 24
        let fullWidth = contentStack.widthAnchor.constraint(
            equalTo: scrollView.frameLayoutGuide.widthAnchor, constant: -sidePadding * 2)
        fullWidth.priority = .defaultHigh          // yields to the 620 cap when wide
        let maxWidth = contentStack.widthAnchor.constraint(lessThanOrEqualToConstant: 620)

        NSLayoutConstraint.activate([
            scrollView.topAnchor.constraint(equalTo: view.safeAreaLayoutGuide.topAnchor),
            scrollView.leadingAnchor.constraint(equalTo: view.leadingAnchor),
            scrollView.trailingAnchor.constraint(equalTo: view.trailingAnchor),
            scrollView.bottomAnchor.constraint(equalTo: beginButton.topAnchor, constant: -16),

            // Vertical: top/bottom against the content guide give scroll height.
            contentStack.topAnchor.constraint(
                equalTo: scrollView.contentLayoutGuide.topAnchor, constant: 28),
            contentStack.bottomAnchor.constraint(
                equalTo: scrollView.contentLayoutGuide.bottomAnchor, constant: -28),
            // Horizontal: center within the content guide, but keep both edges
            // inside the *frame* guide so nothing can spill past the sheet.
            contentStack.centerXAnchor.constraint(
                equalTo: scrollView.frameLayoutGuide.centerXAnchor),
            contentStack.leadingAnchor.constraint(
                greaterThanOrEqualTo: scrollView.frameLayoutGuide.leadingAnchor, constant: sidePadding),
            contentStack.trailingAnchor.constraint(
                lessThanOrEqualTo: scrollView.frameLayoutGuide.trailingAnchor, constant: -sidePadding),
            fullWidth,
            maxWidth,

            beginButton.centerXAnchor.constraint(equalTo: view.centerXAnchor),
            beginButton.widthAnchor.constraint(equalToConstant: 200),
            beginButton.heightAnchor.constraint(equalToConstant: 52),
            beginButton.bottomAnchor.constraint(
                equalTo: view.safeAreaLayoutGuide.bottomAnchor, constant: -20),
        ])
    }

    // MARK: - Content

    private func buildContent() {
        // Masthead.
        contentStack.addArrangedSubview(makeTitleLabel("MARK I PERCEPTRON"))
        contentStack.addArrangedSubview(
            makeSubtitleLabel("A LEARNING MACHINE · ROSENBLATT · 1958"))
        contentStack.addArrangedSubview(makeRule())

        // What it does.
        addSection(
            heading: "WHAT THIS MACHINE DOES",
            body: """
            This is a working recreation of the first machine that could \
            learn. You give it a pattern by flipping the input switches, then \
            tell it whether the answer should be positive or negative by \
            pressing LEARN + or LEARN −. With each lesson it nudges its weight \
            dials, and the analog meter swings a little closer to the right \
            answer.

            Nothing is programmed by hand. Flip switches, turn dials, press \
            LEARN, and watch a machine teach itself to tell one pattern from \
            another — the same idea that grew into today's neural networks.
            """)

        // History.
        addSection(
            heading: "A LITTLE HISTORY",
            body: """
            In 1958, psychologist Frank Rosenblatt (1928–1971) at the Cornell \
            Aeronautical Laboratory built the Mark I Perceptron — a room-sized \
            machine of photocells, wires, and motor-driven dials that could \
            learn to recognize patterns by trial and error, without being \
            explicitly programmed.

            It was the first artificial neural network, and it caused a \
            sensation. The New York Times reported the Navy expected it would \
            one day "walk, talk, see, write, reproduce itself and be conscious \
            of its existence." The perceptron is the direct ancestor of every \
            neural network in use today.
            """)

        // Inventory — illustrated with cropped shots of the real controls so
        // a first-time user can match each description to what's on the panel.
        addSection(
            heading: "PANEL INVENTORY",
            body: """
            The real Mark I had three layers of units — sensory (S), \
            association (A), and response (R). This panel puts that same idea \
            in front of you. Here's what each control does:
            """)

        addIllustratedItem(
            image: "TutorialSwitches",
            imageHeight: 200,
            title: "INPUT SWITCHES",
            detail: "A 4×4 grid of toggles — this is the pattern you feed the "
                + "machine. Each switch is ON = +1 or OFF = −1. The d-pad below "
                + "shifts the whole pattern around the grid.")

        addIllustratedItem(
            image: "TutorialKnobs",
            imageHeight: 190,
            title: "WEIGHT KNOBS",
            detail: "One dial per switch (−30 to +30). These are the machine's "
                + "memory: how strongly each input pushes the output. You rarely "
                + "turn them by hand — pressing LEARN turns them for you.")

        // BIAS and RATE are the two small knobs — shown as a matched pair,
        // equal width and centered, exactly as they sit on the panel.
        addPairedItem(
            leftImage: "TutorialBias", leftCaption: "BIAS",
            rightImage: "TutorialRate", rightCaption: "RATE",
            imageHeight: 150,
            detail: "BIAS shifts the machine's threshold up or down. RATE sets "
                + "how far each LEARN press nudges the weights (it starts at 10).")

        addIllustratedItem(
            image: "TutorialMeter",
            imageHeight: 150,
            title: "ANALOG METER & OUTPUT LED",
            detail: "The needle shows the live output — SUM(switch × weight) + "
                + "bias — swinging positive or negative. The LED below lights "
                + "green whenever the machine's answer comes out positive.")

        addBullets([
            ("LEARN + / LEARN −", "Tell the machine the answer for the current pattern should be positive or negative. It adjusts the weights toward that."),
            ("RESET", "Clear the weights and start teaching from scratch."),
        ])

        // Links.
        addSection(heading: "LEARN MORE", body: nil)
        addLink("Perceptron — Wikipedia",
                url: "https://en.wikipedia.org/wiki/Perceptron")
        addLink("Frank Rosenblatt — Wikipedia",
                url: "https://en.wikipedia.org/wiki/Frank_Rosenblatt")
        addLink("Mark I Perceptron — Wikipedia",
                url: "https://en.wikipedia.org/wiki/Mark_I_Perceptron")
        addLink("Welch Labs — the video that inspired this project",
                url: "https://www.youtube.com/watch?v=l-9ALe3U-Fg")
    }

    // MARK: - Section builders

    private func addSection(heading: String, body: String?) {
        contentStack.addArrangedSubview(makeHeadingLabel(heading))
        if let body {
            contentStack.addArrangedSubview(makeBodyLabel(body))
        }
    }

    /// A single control illustration: a centered, aspect-fit screenshot of the
    /// real control, then a bold title and a description beneath it.
    private func addIllustratedItem(image name: String, imageHeight: CGFloat,
                                    title: String, detail: String) {
        let item = UIStackView()
        item.axis = .vertical
        item.alignment = .fill
        item.spacing = 8

        item.addArrangedSubview(centered(makeTutorialImageView(name, height: imageHeight)))

        let titleLabel = UILabel()
        titleLabel.text = title
        titleLabel.font = Font.mono(13, .bold)
        titleLabel.textColor = Palette.engraved
        titleLabel.numberOfLines = 0
        item.addArrangedSubview(titleLabel)

        item.addArrangedSubview(makeDetailLabel(detail))
        contentStack.addArrangedSubview(item)
    }

    /// Two equally-sized small controls shown side by side (BIAS + RATE), each
    /// with a caption beneath it, then a shared description. The pair is
    /// centered as a unit and the two images share one width so they always
    /// match, mirroring how they sit on the panel.
    private func addPairedItem(leftImage: String, leftCaption: String,
                               rightImage: String, rightCaption: String,
                               imageHeight: CGFloat, detail: String) {
        let left = captionedImage(leftImage, caption: leftCaption, height: imageHeight)
        let right = captionedImage(rightImage, caption: rightCaption, height: imageHeight)

        // `.fillEqually` already forces the two columns to equal width once they
        // share the row as an ancestor — so BIAS and RATE always match. (An
        // explicit width-equality constraint here would crash: the two views
        // have no common ancestor until the row stack view is built below.)
        let row = UIStackView(arrangedSubviews: [left, right])
        row.axis = .horizontal
        row.alignment = .top
        row.distribution = .fillEqually
        row.spacing = 24

        let item = UIStackView(arrangedSubviews: [centered(row), makeDetailLabel(detail)])
        item.axis = .vertical
        item.alignment = .fill
        item.spacing = 8
        contentStack.addArrangedSubview(item)
    }

    /// A control image with a small metal-style caption plate beneath it,
    /// both centered in a vertical column.
    private func captionedImage(_ name: String, caption: String, height: CGFloat) -> UIView {
        let column = UIStackView()
        column.axis = .vertical
        column.alignment = .center
        column.spacing = 6
        column.addArrangedSubview(makeTutorialImageView(name, height: height))

        let label = UILabel()
        label.text = caption
        label.font = Font.mono(12, .bold)
        label.textColor = Palette.engravedDim
        label.textAlignment = .center
        column.addArrangedSubview(label)
        return column
    }

    /// Wraps a view so it sits centered at its intrinsic width inside a
    /// full-width row, rather than being stretched to fill.
    private func centered(_ inner: UIView) -> UIView {
        let wrapper = UIView()
        inner.translatesAutoresizingMaskIntoConstraints = false
        wrapper.addSubview(inner)
        NSLayoutConstraint.activate([
            inner.topAnchor.constraint(equalTo: wrapper.topAnchor),
            inner.bottomAnchor.constraint(equalTo: wrapper.bottomAnchor),
            inner.centerXAnchor.constraint(equalTo: wrapper.centerXAnchor),
            inner.leadingAnchor.constraint(greaterThanOrEqualTo: wrapper.leadingAnchor),
            inner.trailingAnchor.constraint(lessThanOrEqualTo: wrapper.trailingAnchor),
        ])
        return wrapper
    }

    private func makeTutorialImageView(_ name: String, height: CGFloat) -> UIImageView {
        let image = UIImage(named: name)
        let iv = UIImageView(image: image)
        iv.contentMode = .scaleAspectFit
        iv.clipsToBounds = true
        iv.layer.cornerRadius = 6
        iv.layer.borderWidth = 1
        iv.layer.borderColor = Palette.hairline.cgColor
        iv.translatesAutoresizingMaskIntoConstraints = false

        // Aspect ratio is required and always holds. Height is the *preferred*
        // size (high priority, not required) so that on a narrow window the
        // wrapper's width cap can shrink the image — height follows via the
        // ratio — instead of the layout becoming unsatisfiable. On a wide
        // window the image renders at exactly `height`.
        if let size = image?.size, size.height > 0 {
            let aspect = iv.widthAnchor.constraint(equalTo: iv.heightAnchor,
                                                   multiplier: size.width / size.height)
            aspect.priority = .required
            aspect.isActive = true
        }
        let preferredHeight = iv.heightAnchor.constraint(equalToConstant: height)
        preferredHeight.priority = .defaultHigh
        preferredHeight.isActive = true
        iv.heightAnchor.constraint(lessThanOrEqualToConstant: height).isActive = true
        return iv
    }

    private func makeDetailLabel(_ text: String) -> UILabel {
        let label = UILabel()
        label.numberOfLines = 0
        let paragraph = NSMutableParagraphStyle()
        paragraph.lineSpacing = 3
        label.attributedText = NSAttributedString(
            string: text,
            attributes: [
                .font: Font.mono(13),
                .foregroundColor: Palette.engravedDim,
                .paragraphStyle: paragraph,
            ])
        return label
    }

    private func addBullets(_ items: [(String, String)]) {
        for (term, detail) in items {
            let label = UILabel()
            label.numberOfLines = 0
            let text = NSMutableAttributedString(
                string: "• \(term)  ",
                attributes: [.font: Font.mono(13, .bold), .foregroundColor: Palette.engraved])
            text.append(NSAttributedString(
                string: detail,
                attributes: [.font: Font.mono(13), .foregroundColor: Palette.engravedDim]))
            let paragraph = NSMutableParagraphStyle()
            paragraph.lineSpacing = 3
            paragraph.firstLineHeadIndent = 0
            paragraph.headIndent = 14
            text.addAttribute(.paragraphStyle, value: paragraph,
                              range: NSRange(location: 0, length: text.length))
            label.attributedText = text
            contentStack.addArrangedSubview(label)
        }
    }

    private func addLink(_ title: String, url: String) {
        let button = UIButton(type: .system)
        button.contentHorizontalAlignment = .leading
        button.titleLabel?.font = Font.mono(14, .semibold)
        button.titleLabel?.numberOfLines = 0
        button.setTitleColor(Palette.link, for: .normal)
        button.setTitle("→  \(title)", for: .normal)
        button.accessibilityHint = "Opens in your browser"
        if let target = URL(string: url) {
            button.addAction(UIAction { [weak self] _ in
                self?.open(target)
            }, for: .touchUpInside)
        }
        contentStack.addArrangedSubview(button)
    }

    // MARK: - Label factories

    private func makeTitleLabel(_ text: String) -> UILabel {
        let label = UILabel()
        label.text = text
        label.font = Font.mono(24, .heavy)
        label.textColor = Palette.engraved
        label.numberOfLines = 0
        label.adjustsFontSizeToFitWidth = true
        label.minimumScaleFactor = 0.6
        return label
    }

    private func makeSubtitleLabel(_ text: String) -> UILabel {
        let label = UILabel()
        label.text = text
        label.font = Font.mono(12, .semibold)
        label.textColor = Palette.engravedDim
        label.numberOfLines = 0
        return label
    }

    private func makeHeadingLabel(_ text: String) -> UILabel {
        let label = UILabel()
        label.text = text
        label.font = Font.mono(15, .bold)
        label.textColor = Palette.heading
        label.numberOfLines = 0
        return label
    }

    private func makeBodyLabel(_ text: String) -> UILabel {
        let label = UILabel()
        label.numberOfLines = 0
        let paragraph = NSMutableParagraphStyle()
        paragraph.lineSpacing = 4
        paragraph.paragraphSpacing = 8
        label.attributedText = NSAttributedString(
            string: text,
            attributes: [
                .font: Font.mono(13),
                .foregroundColor: Palette.engravedDim,
                .paragraphStyle: paragraph,
            ])
        return label
    }

    private func makeRule() -> UIView {
        let line = UIView()
        line.backgroundColor = Palette.hairline
        line.translatesAutoresizingMaskIntoConstraints = false
        line.heightAnchor.constraint(equalToConstant: 1).isActive = true
        return line
    }

    // MARK: - Actions

    private func open(_ url: URL) {
        UIApplication.shared.open(url)
    }

    private func dismissIntro() {
        dismiss(animated: true)
    }
}
