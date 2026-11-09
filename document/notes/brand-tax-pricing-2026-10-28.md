# Thuế Brand: mua gói và ký quỹ

Run SQL 166 before deploying app binaries, after existing package/funding scripts. No existing order amounts are recalculated. Existing tax snapshots default to None/0. Default new behavior remains unchanged until Admin configures VAT.

Admin → Tài chính → Thuế Brand. Each save creates an immutable version with effective date in Vietnam time (UTC+7). Package VAT applies to package sale price. Funding VAT applies to either TopStar service fee or campaign budget plus service fee according to the accounting model. Gateway fee uses existing campaign fee policy; VAT base excludes gateway fees. No automatic KOC personal income tax deduction is added at Brand checkout.

Included: VAT = round(base × rate / (100+rate)), payable stays unchanged.
Excluded: VAT = round(base × rate /100), payable increases by VAT.
None: VAT = 0.
All VND rounding uses AwayFromZero. Policy ID/rate/mode/VAT/added VAT saved on purchases and fundings. Gateway verification and balance payments use saved total. Package refund uses full paid snapshot including VAT. Campaign escrow refunds remain budget-only; service/gateway/tax refunds require separate accounting review.

Package checkout and funding confirmation verify displayed expected total before creation. New policy versions do not change saved orders. Existing package catalog sale prices are base prices; final VAT is displayed at checkout.

Deploy SQL166 manually; the agent does not migrate the user database. Back up and apply schema before starting new binaries.
