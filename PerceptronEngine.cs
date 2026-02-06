using PerceptronSimulator.Controls;

namespace PerceptronSimulator;

/// <summary>
/// Neural network engine supporting multiple learning rules from 1958-1986.
/// </summary>
public class PerceptronEngine
{
    public int GridSize { get; private set; }
    public double[] Weights { get; private set; }  // W^(2): hidden→output weights (shown on knobs)
    public double Bias { get; set; }               // Output bias
    public double LearningRate { get; set; } = 10.0;

    // Math rule selection
    public ConfigKnob.MathRule MathRule { get; set; } = ConfigKnob.MathRule.PERCEPTRON_CLASSIC;

    // Multi-layer mode (used by BACKPROP rule)
    public double[,]? HiddenWeights { get; private set; }  // W^(1): input→hidden weights (n×n matrix)
    public double[]? HiddenBiases { get; private set; }    // Hidden layer biases
    public double[]? HiddenOutputs { get; private set; }   // Cached hidden layer outputs

    public PerceptronEngine(int gridSize)
    {
        GridSize = gridSize;
        Weights = new double[gridSize * gridSize];
        Bias = 0;
        InitializeMultiLayerWeights(gridSize * gridSize);
    }

    public void Resize(int newNodeCount)
    {
        GridSize = (int)Math.Sqrt(newNodeCount);
        Weights = new double[newNodeCount];
        Bias = 0;
        InitializeMultiLayerWeights(newNodeCount);
    }

    private void InitializeMultiLayerWeights(int nodeCount)
    {
        // Initialize W^(1) with small random values for BACKPROP mode
        var rand = new Random();
        HiddenWeights = new double[nodeCount, nodeCount];
        HiddenBiases = new double[nodeCount];
        HiddenOutputs = new double[nodeCount];

        for (int j = 0; j < nodeCount; j++)
        {
            HiddenBiases[j] = 0;
            for (int i = 0; i < nodeCount; i++)
            {
                // Small random weights for symmetry breaking
                HiddenWeights[j, i] = (rand.NextDouble() - 0.5) * 0.1;
            }
        }
    }

    /// <summary>
    /// Calculate the network output based on the selected MathRule.
    /// </summary>
    public double CalculateOutput(int[] inputs)
    {
        if (inputs.Length != Weights.Length)
            throw new ArgumentException("Input size must match weight count");

        return MathRule switch
        {
            // 1958: Original Rosenblatt perceptron (1-to-1 connectivity)
            // PERCEPTRON_CLASSIC: OUTPUT = Σ(input[i] × weight[i]) + bias
            ConfigKnob.MathRule.PERCEPTRON_CLASSIC => CalculateClassicPerceptron(inputs),

            // 1958+: 1958_SUM_RULE - Fully connected, each hidden node sums all weighted inputs
            // h[j] = Σᵢ(input[i] × weight[j]) then OUTPUT = Σ(h[j]) + bias
            ConfigKnob.MathRule.RULE_1958_SUM => Calculate1958Sum(inputs),

            // 1958m: 1958_AVG_RULE - Fully connected, each hidden node averages all weighted inputs
            // h[j] = (1/N) × Σᵢ(input[i] × weight[j]) then OUTPUT = Σ(h[j]) + bias
            ConfigKnob.MathRule.RULE_1958_AVG => Calculate1958Avg(inputs),

            // 1958/+: 1958_DIV_SUM_RULE - Divide inputs by N, then sum
            // h[j] = Σᵢ((input[i]/N) × weight[j]) then OUTPUT = Σ(h[j]) + bias
            ConfigKnob.MathRule.RULE_1958_DIV_SUM => Calculate1958DivSum(inputs),

            // 1958/m: 1958_DIV_AVG_RULE - Divide inputs by N, then average
            // h[j] = (1/N) × Σᵢ((input[i]/N) × weight[j]) then OUTPUT = Σ(h[j]) + bias
            ConfigKnob.MathRule.RULE_1958_DIV_AVG => Calculate1958DivAvg(inputs),

            // 1960: WIDROW_HOFF - Widrow-Hoff/LMS/Delta rule
            // Same 1-to-1 topology as classic perceptron, but different learning rule
            ConfigKnob.MathRule.WIDROW_HOFF => CalculateClassicPerceptron(inputs),

            // 1986: BACKPROP - Multi-layer with ReLU and backpropagation
            // h[j] = ReLU(Σᵢ(W¹[j,i] × input[i]) + b¹[j]) then OUTPUT = Σ(W²[j] × h[j]) + bias
            ConfigKnob.MathRule.BACKPROP => CalculateBackpropOutput(inputs),

            _ => CalculateClassicPerceptron(inputs)
        };
    }

    /// <summary>
    /// PERCEPTRON_CLASSIC (1958): Original Rosenblatt perceptron
    /// 1-to-1 connectivity: each input connects to exactly one hidden node
    /// OUTPUT = Σ(input[i] × weight[i]) + bias
    /// </summary>
    private double CalculateClassicPerceptron(int[] inputs)
    {
        int n = inputs.Length;
        HiddenOutputs = new double[n];

        double sum = 0;
        for (int i = 0; i < n; i++)
        {
            // Each hidden node receives only its corresponding input
            HiddenOutputs[i] = inputs[i] * Weights[i];
            sum += HiddenOutputs[i];
        }
        sum += Bias;

        return sum;
    }

    /// <summary>
    /// 1958_SUM_RULE (1958+): Fully connected, sum rule
    /// Each hidden node receives ALL inputs and sums them
    /// h[j] = Σᵢ(input[i] × weight[j])
    /// OUTPUT = Σ(h[j]) + bias
    /// </summary>
    private double Calculate1958Sum(int[] inputs)
    {
        int n = inputs.Length;
        HiddenOutputs = new double[n];

        double output = 0;
        for (int j = 0; j < n; j++)
        {
            // Each hidden node j receives all inputs, weighted by weight[j]
            double h = 0;
            for (int i = 0; i < n; i++)
            {
                h += inputs[i] * Weights[j];
            }
            HiddenOutputs[j] = h;
            output += h;
        }
        output += Bias;

        return output;
    }

    /// <summary>
    /// 1958_AVG_RULE (1958m): Fully connected, average rule
    /// Each hidden node receives ALL inputs and averages them
    /// h[j] = (1/N) × Σᵢ(input[i] × weight[j])
    /// OUTPUT = Σ(h[j]) + bias
    /// </summary>
    private double Calculate1958Avg(int[] inputs)
    {
        int n = inputs.Length;
        HiddenOutputs = new double[n];

        double output = 0;
        for (int j = 0; j < n; j++)
        {
            double h = 0;
            for (int i = 0; i < n; i++)
            {
                h += inputs[i] * Weights[j];
            }
            // Average by dividing by number of inputs
            h /= n;
            HiddenOutputs[j] = h;
            output += h;
        }
        output += Bias;

        return output;
    }

    /// <summary>
    /// 1958_DIV_SUM_RULE (1958/+): Divide inputs, then sum
    /// Each hidden node receives ALL inputs divided by N, then sums
    /// h[j] = Σᵢ((input[i]/N) × weight[j])
    /// OUTPUT = Σ(h[j]) + bias
    /// </summary>
    private double Calculate1958DivSum(int[] inputs)
    {
        int n = inputs.Length;
        HiddenOutputs = new double[n];

        double output = 0;
        for (int j = 0; j < n; j++)
        {
            double h = 0;
            for (int i = 0; i < n; i++)
            {
                // Divide each input by N before weighting
                h += (inputs[i] / (double)n) * Weights[j];
            }
            HiddenOutputs[j] = h;
            output += h;
        }
        output += Bias;

        return output;
    }

    /// <summary>
    /// 1958_DIV_AVG_RULE (1958/m): Divide inputs, then average
    /// Each hidden node receives ALL inputs divided by N, weights them, then averages
    /// h[j] = (1/N) × Σᵢ((input[i]/N) × weight[j])
    /// OUTPUT = Σ(h[j]) + bias
    /// </summary>
    private double Calculate1958DivAvg(int[] inputs)
    {
        int n = inputs.Length;
        HiddenOutputs = new double[n];

        double output = 0;
        for (int j = 0; j < n; j++)
        {
            double h = 0;
            for (int i = 0; i < n; i++)
            {
                // Divide each input by N before weighting
                h += (inputs[i] / (double)n) * Weights[j];
            }
            // Then average the result
            h /= n;
            HiddenOutputs[j] = h;
            output += h;
        }
        output += Bias;

        return output;
    }

    /// <summary>
    /// BACKPROP (1986): Multi-layer perceptron with ReLU and learned hidden weights
    /// h[j] = ReLU(Σᵢ(W¹[j,i] × input[i]) + b¹[j])
    /// OUTPUT = Σ(W²[j] × h[j]) + bias
    /// </summary>
    private double CalculateBackpropOutput(int[] inputs)
    {
        if (HiddenWeights == null || HiddenBiases == null || HiddenOutputs == null)
            return CalculateClassicPerceptron(inputs);

        int n = inputs.Length;

        // Forward pass: Input → Hidden layer
        // z^(1) = W^(1) * x + b^(1)
        // h^(1) = ReLU(z^(1))
        for (int j = 0; j < n; j++)
        {
            double z = HiddenBiases[j];
            for (int i = 0; i < n; i++)
            {
                z += HiddenWeights[j, i] * inputs[i];
            }
            // ReLU activation
            HiddenOutputs[j] = Math.Max(0, z);
        }

        // Forward pass: Hidden → Output layer
        // output = W^(2) * h^(1) + bias
        double output = Bias;
        for (int j = 0; j < n; j++)
        {
            output += Weights[j] * HiddenOutputs[j];
        }

        return output;
    }

    /// <summary>
    /// Apply the appropriate learning rule based on MathRule setting.
    /// </summary>
    /// <param name="inputs">Current switch states (-1 or +1)</param>
    /// <param name="desiredPositive">True if output should be positive, false if negative</param>
    /// <returns>True if weights were adjusted, false if already correct</returns>
    public bool Learn(int[] inputs, bool desiredPositive)
    {
        double currentOutput = CalculateOutput(inputs);

        // Check if output is already correct
        if (desiredPositive && currentOutput > 0)
            return false; // Already positive, no adjustment needed
        if (!desiredPositive && currentOutput < 0)
            return false; // Already negative, no adjustment needed

        return MathRule switch
        {
            // 1958 rules: Use original perceptron learning (update only when wrong)
            ConfigKnob.MathRule.PERCEPTRON_CLASSIC => LearnClassicPerceptron(inputs, desiredPositive),
            ConfigKnob.MathRule.RULE_1958_SUM => LearnClassicPerceptron(inputs, desiredPositive),
            ConfigKnob.MathRule.RULE_1958_AVG => LearnClassicPerceptron(inputs, desiredPositive),
            ConfigKnob.MathRule.RULE_1958_DIV_SUM => LearnClassicPerceptron(inputs, desiredPositive),
            ConfigKnob.MathRule.RULE_1958_DIV_AVG => LearnClassicPerceptron(inputs, desiredPositive),

            // 1960: Widrow-Hoff/LMS rule (continuous error, MSE minimization)
            ConfigKnob.MathRule.WIDROW_HOFF => LearnWidrowHoff(inputs, desiredPositive, currentOutput),

            // 1986: Backpropagation
            ConfigKnob.MathRule.BACKPROP => LearnBackprop(inputs, desiredPositive, currentOutput),

            _ => LearnClassicPerceptron(inputs, desiredPositive)
        };
    }

    /// <summary>
    /// Original perceptron learning rule (1958).
    /// Updates weights only when classification is wrong.
    /// Uses the sign of the output (quantized) for error calculation.
    /// </summary>
    private bool LearnClassicPerceptron(int[] inputs, bool desiredPositive)
    {
        // Single-layer perceptron learning rule (original behavior)
        for (int i = 0; i < inputs.Length; i++)
        {
            if (desiredPositive)
            {
                // Want positive output:
                // - Turn UP dials for ON switches (input = +1)
                // - Turn DOWN dials for OFF switches (input = -1)
                Weights[i] += LearningRate * inputs[i];
            }
            else
            {
                // Want negative output:
                // - Turn DOWN dials for ON switches (input = +1)
                // - Turn UP dials for OFF switches (input = -1)
                Weights[i] -= LearningRate * inputs[i];
            }

            // Clamp weights to valid range
            Weights[i] = Math.Clamp(Weights[i], -30, 30);
        }

        // Adjust bias similarly
        if (desiredPositive)
            Bias += LearningRate;
        else
            Bias -= LearningRate;

        Bias = Math.Clamp(Bias, -30, 30);

        return true;
    }

    /// <summary>
    /// WIDROW_HOFF (1960): Widrow-Hoff / LMS / Delta rule
    /// w(t+1) = w(t) + η × (d(t) - y(t)) × x(t)
    /// Uses continuous error from linear output (not quantized).
    /// Minimizes Mean Squared Error via gradient descent.
    /// </summary>
    private bool LearnWidrowHoff(int[] inputs, bool desiredPositive, double currentOutput)
    {
        // Target: we want positive (e.g., +10) or negative (e.g., -10) output
        double target = desiredPositive ? 10.0 : -10.0;
        double error = target - currentOutput;

        // Scale learning rate for LMS (original rate is designed for perceptron rule)
        double lr = LearningRate * 0.1;

        // Widrow-Hoff rule: w(t+1) = w(t) + η × error × x(t)
        for (int i = 0; i < inputs.Length; i++)
        {
            Weights[i] += lr * error * inputs[i];
            Weights[i] = Math.Clamp(Weights[i], -30, 30);
        }

        // Bias update: same rule with input = 1
        Bias += lr * error;
        Bias = Math.Clamp(Bias, -30, 30);

        return true;
    }

    /// <summary>
    /// BACKPROP (1986): Backpropagation for multi-layer perceptron.
    /// Uses gradient descent with chain rule to update hidden layer weights.
    /// </summary>
    private bool LearnBackprop(int[] inputs, bool desiredPositive, double currentOutput)
    {
        if (HiddenWeights == null || HiddenBiases == null || HiddenOutputs == null)
            return LearnClassicPerceptron(inputs, desiredPositive);

        int n = inputs.Length;

        // Target: we want positive (e.g., +10) or negative (e.g., -10) output
        double target = desiredPositive ? 10.0 : -10.0;
        double error = target - currentOutput;

        // Scale learning rate for backprop (original rate is designed for perceptron rule)
        double lr = LearningRate * 0.01;

        // Backpropagation: Output layer
        // ∂L/∂W^(2)_j = error × h^(1)_j
        // ∂L/∂bias = error
        for (int j = 0; j < n; j++)
        {
            Weights[j] += lr * error * HiddenOutputs[j];
            Weights[j] = Math.Clamp(Weights[j], -30, 30);
        }
        Bias += lr * error;
        Bias = Math.Clamp(Bias, -30, 30);

        // Backpropagation: Hidden layer
        // δ^(1)_j = W^(2)_j × error × ReLU'(z^(1)_j)
        // ∂L/∂W^(1)_ji = δ^(1)_j × x_i
        // ∂L/∂b^(1)_j = δ^(1)_j
        for (int j = 0; j < n; j++)
        {
            // ReLU derivative: 1 if output > 0, else 0
            double reluDerivative = HiddenOutputs[j] > 0 ? 1.0 : 0.0;
            double delta = Weights[j] * error * reluDerivative;

            // Update hidden weights
            for (int i = 0; i < n; i++)
            {
                HiddenWeights[j, i] += lr * delta * inputs[i];
                // Clamp hidden weights too
                HiddenWeights[j, i] = Math.Clamp(HiddenWeights[j, i], -30, 30);
            }

            // Update hidden bias
            HiddenBiases[j] += lr * delta;
            HiddenBiases[j] = Math.Clamp(HiddenBiases[j], -30, 30);
        }

        return true;
    }

    public void ResetWeights()
    {
        for (int i = 0; i < Weights.Length; i++)
        {
            Weights[i] = 0;
        }
        Bias = 0;

        // Also reset hidden weights for backprop mode
        if (HiddenWeights != null && HiddenBiases != null)
        {
            InitializeMultiLayerWeights(Weights.Length);
        }
    }

    public void SetWeight(int index, double value)
    {
        if (index >= 0 && index < Weights.Length)
        {
            Weights[index] = Math.Clamp(value, -30, 30);
        }
    }

    public double GetWeight(int index)
    {
        if (index >= 0 && index < Weights.Length)
        {
            return Weights[index];
        }
        return 0;
    }

    /// <summary>
    /// Returns true if the current MathRule uses fully-connected hidden layer
    /// (all inputs connect to all hidden nodes).
    /// Historically accurate: 1958 and 1960 used 1-to-1, only 1958+ variants and 1986 use fully-connected.
    /// </summary>
    public bool IsFullyConnected => MathRule != ConfigKnob.MathRule.PERCEPTRON_CLASSIC
                                 && MathRule != ConfigKnob.MathRule.WIDROW_HOFF;

    /// <summary>
    /// Returns true if the current MathRule uses backpropagation
    /// (has learned hidden weights).
    /// </summary>
    public bool UsesBackprop => MathRule == ConfigKnob.MathRule.BACKPROP;

    // Compatibility property for DebugDialog
    public bool MultiLayerMode => IsFullyConnected;
}
