﻿// Copyright (c) Cedita Ltd. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the solution root for license information.
using Cedita.Payroll.Models.TaxYearSpecifics;
using System;

namespace Cedita.Payroll.Engines.NationalInsurance
{
    [EngineApplicableTaxYear(TaxYearStartYear = 2017)]
    public class NationalInsurance2017 : NationalInsurance2016
    {
        public override NationalInsuranceCalculation CalculateNationalInsurance(decimal gross, char niCategory, PayPeriods periods)
        {
            int periodCnt = 52;
            int weeksInPeriod = 1;
            if (periods == PayPeriods.Monthly)
                periodCnt = 12;
            else
                weeksInPeriod = (int)Math.Round((decimal)periodCnt / (int)periods);

            var totalPT = TaxYearSpecificProvider.GetSpecificValue<decimal>(TaxYearSpecificValues.PrimaryThreshold);
            var totalST = TaxYearSpecificProvider.GetSpecificValue<decimal>(TaxYearSpecificValues.SecondaryThreshold);
            var totalUEL = TaxYearSpecificProvider.GetSpecificValue<decimal>(TaxYearSpecificValues.UpperEarningsLimit);
            var totalLEL = TaxYearSpecificProvider.GetSpecificValue<decimal>(TaxYearSpecificValues.LowerEarningsLimit);
            var totalUST = TaxYearSpecificProvider.GetSpecificValue<decimal>(TaxYearSpecificValues.UpperSecondaryThreshold);
            var totalAUST = TaxYearSpecificProvider.GetSpecificValue<decimal>(TaxYearSpecificValues.ApprenticeUpperSecondaryThreshold);

            var niRates = TaxYearSpecificProvider.GetCodeSpecifics(niCategory);

            // WTF. UEL must round 865.3846 to 866. But PT must round 680.3333 to 680. This isn't sane.
            decimal periodPT = TaxMath.PeriodRound(TaxMath.Factor(totalPT, weeksInPeriod, periodCnt), weeksInPeriod),
                periodUEL = Math.Ceiling(TaxMath.Factor(totalUEL, weeksInPeriod, periodCnt)),
                periodLEL = Math.Ceiling(TaxMath.Factor(totalLEL, weeksInPeriod, periodCnt));

            var niCalc = new NationalInsuranceCalculation
            {
                EarningsUptoIncludingLEL = SubtractRound(gross, periodLEL, 0),
                EarningsAboveLELUptoIncludingPT = SubtractRound(gross, periodPT, periodLEL),
                EarningsAboveSTUptoIncludingUEL = SubtractRound(gross, periodUEL, periodPT),
                EarningsAboveUEL = SubtractRound(gross, gross, periodUEL)
            };

            niCalc.EmployeeNiGross += TaxMath.HmrcRound(niCalc.EarningsAboveSTUptoIncludingUEL * (niRates.EeC / 100));
            niCalc.EmployeeNiGross += TaxMath.HmrcRound(niCalc.EarningsAboveUEL * (niRates.EeD / 100));

            niCalc.EmployerNiGross += TaxMath.HmrcRound(niCalc.EarningsAboveSTUptoIncludingUEL * (niRates.ErC / 100));
            niCalc.EmployerNiGross += TaxMath.HmrcRound(niCalc.EarningsAboveUEL * (niRates.ErD / 100));

            return niCalc;
        }
    }
}
