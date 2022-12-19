import unittest
import xml.parsers.expat

from tests.base import Runner


class WebApiSecurityRuleTests(unittest.TestCase):

    def test_cookieless_useuri(self):
        analysis = Runner.fetch_analysis_result('CookielessUseUri/CookielessUseUri.csproj')
        file_object = analysis.get_object_by_name('Web.config', 'CAST_DotNet_SourceFile')
        violations = analysis.get_violations(file_object, 'EIDotNetQualityRules.EnsureCookielessAreSetToUseCookies')
        self.assertEqual(2, len(violations))

    def test_cookieless_unset(self):
        analysis = Runner.fetch_analysis_result('CookielessUnset/CookielessUnset.csproj')
        file_object = analysis.get_object_by_name('Web.config', 'CAST_DotNet_SourceFile')
        violations = analysis.get_violations(file_object, 'EIDotNetQualityRules.EnsureCookielessAreSetToUseCookies')
        self.assertEqual(1, len(violations))

    def test_cookieless_remediation(self):
        analysis = Runner.fetch_analysis_result('CookielessRemediation/CookielessRemediation.csproj')
        file_object = analysis.get_object_by_name('Web.config', 'CAST_DotNet_SourceFile')
        self.assertTrue(file_object)
        violations = analysis.get_violations(file_object, 'EIDotNetQualityRules.EnsureCookielessAreSetToUseCookies')
        self.assertEqual(0, len(violations))
