using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsAndroid.Entities
{
	internal class Spring
	{
		public double Velocity { get; set; }
		public double DampingRatio { get; set; }
		public double Frequency { get; set; }

		public bool Sleeping { get; set; }

		public double Position { get; set; }
		public double Final { get; set; }

		public Spring(double initial, double dampingRatio, double frequency)
		{
			if (dampingRatio * frequency < 0)
				throw new Exception("Spring does not converge");

			DampingRatio = dampingRatio;
			Frequency = frequency;
			Velocity = 0;
			Position = initial;
			Final = initial;
		}

		public double Update(double deltaTime)
		{
			double radialFrequency = (this.Frequency * Math.Tau);
			double final = this.Final;
			double velocity = this.Velocity;

			double offset = (this.Position - final);
			double dampingRatio = this.DampingRatio;
			double decay = Math.Exp(-dampingRatio * radialFrequency * deltaTime);

			double newPosition;
			double newVelocity;

			if (this.DampingRatio == 1)
			{
				newPosition = (((offset * (1 + radialFrequency * deltaTime) + velocity * deltaTime) * decay) + final);
				newVelocity = ((velocity * (1 - radialFrequency * deltaTime) - offset * (radialFrequency * radialFrequency * deltaTime)) * decay);
			}
			else if (this.DampingRatio < 1)
			{
				double c = Math.Sqrt(1 - (dampingRatio * dampingRatio));

				double i = Math.Cos(radialFrequency * c * deltaTime);
				double j = Math.Sin(radialFrequency * c * deltaTime);

				double z;
				if (c > 1e-4)
					z = j / c;
				else
				{
					double a = (deltaTime * radialFrequency);
					z = (a + ((((a * a) * (c * c) * (c * c) / 20 - c * c) * (a * a * a)) / 6));
				}

				double y;
				if ((radialFrequency * c) > 1e-4)
					y = (j / (radialFrequency * c));
				else
				{
					double b = (radialFrequency * c);
					y = (deltaTime + ((((deltaTime * deltaTime) * (b * b) * (b * b) / 20 - b * b) * (deltaTime * deltaTime * deltaTime)) / 6));
				}

				newPosition = (((offset * (i + dampingRatio * z) + velocity * y) * decay) + final);
				newVelocity = ((velocity * (i - z * dampingRatio) - offset * (z * radialFrequency)) * decay);
			}
			else
			{
				double c = Math.Sqrt((dampingRatio * dampingRatio) - 1);

				double r1 = (-radialFrequency * (dampingRatio - c));
				double r2 = (-radialFrequency * (dampingRatio + c));

				double co2 = ((velocity - offset * r1) / (2 * radialFrequency * c));
				double co1 = (offset - co2);

				double e1 = (co1 * Math.Exp(r1 * deltaTime));
				double e2 = (co2 * Math.Exp(r2 * deltaTime));

				newPosition = (e1 + e2 + final);
				newVelocity = ((e1 * r1) + (e2 * r2));
			}

			this.Position = newPosition;
			this.Velocity = newVelocity;

			this.Sleeping = (Math.Abs(final - newPosition) <= (double)0.1);

			return newPosition;
		}

		public double UpdateOld(double deltaTime)
		{
			double radialFrequency = Frequency * Math.Tau;
			double final = Final;
			double velocity = Velocity;

			double offset = Position - final;
			double dampingRatio = DampingRatio;
			double decay = Math.Exp(-dampingRatio * radialFrequency * deltaTime);

			double newPosition;
			double newVelocity;

			if (DampingRatio == 1)
			{
				newPosition = (offset * (1 + radialFrequency * deltaTime) + velocity * deltaTime) * decay + final;
				newVelocity = (velocity * (1 - radialFrequency * deltaTime) - offset * (radialFrequency * radialFrequency * deltaTime)) * decay;
			}
			else if (DampingRatio < 1)
			{
				double c = Math.Sqrt(1 - (dampingRatio * dampingRatio));

				double i = Math.Cos(radialFrequency * c * deltaTime);
				double j = Math.Sin(radialFrequency * c * deltaTime);

				double z;
				if (c > 1e-4)
					z = j / c;
				else
				{
					double a = deltaTime * radialFrequency;
					z = (a + ((((a * a) * (c * c) * (c * c) / 20 - c * c) * (a * a * a)) / 6));
				}

				double y;
				if (radialFrequency * c > 1e-4)
					y = j / (radialFrequency * c);
				else
				{
					double b = radialFrequency * c;
					y = (deltaTime + ((((deltaTime * deltaTime) * (b * b) * (b * b) / 20 - b * b) * (deltaTime * deltaTime * deltaTime)) / 6));
				}

				newPosition = (((offset * (i + dampingRatio * z) + velocity * y) * decay) + final);
				newVelocity = ((velocity * (i - z * dampingRatio) - offset * (z * radialFrequency)) * decay);
			}
			else
			{
				double c = Math.Sqrt((dampingRatio * dampingRatio) - 1);

				double r1 = -radialFrequency * (dampingRatio - c);
				double r2 = -radialFrequency * (dampingRatio + c);

				double co2 = (velocity - offset * r1) / (2 * radialFrequency * c);
				double co1 = offset - co2;

				double e1 = co1 * Math.Exp(r1 * deltaTime);
				double e2 = co2 * Math.Exp(r2 * deltaTime);

				newPosition = (e1 + e2 + final);
				newVelocity = (e1 * r1) + (e2 * r2);
			}

			Position = newPosition;
			Velocity = newVelocity;

			Sleeping = Math.Abs(final - newPosition) <= 0.1;

			return newPosition;
		}

		public void Set(double value)
		{
			Position = value;
			Final = value;
			Velocity = 0;

			Sleeping = true;
		}

		public void SetFrequency(double value)
		{
			if (DampingRatio * value < 0)
				throw new Exception("Spring does not converge");

			Frequency = value;
		}

		public void SetDampingRatio(double value)
		{
			if (value * Frequency < 0)
				throw new Exception("Spring does not converge");

			DampingRatio = value;
		}
	}
}