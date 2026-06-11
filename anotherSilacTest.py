@staticmethod
def get_personel(personel_number: int, agreement_id: Optional[int] = None):
extra_filter = f'?agreementId={agreement_id}' if agreement_id is not None else ''
endpoint = F'{settings.BASE_URL}/Agent/{personel_number}/downline{extra_filter}'
result = requests.get(endpoint)
if result.status_code == status.HTTP_200_OK:
return ThirdPartyApi._get_downline_dict(result.content)
raise Exception("Failed to get all associated Personel")
